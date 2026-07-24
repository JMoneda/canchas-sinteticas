using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de partidos abiertos (matchmaking): abrir un partido, unirse, salir (con reembolso de
/// la parte si aplica) y pagar la parte del pago dividido a través de la pasarela real.
/// </summary>
public class MatchService(
    IMatchRepository matches,
    IReservationRepository reservations,
    ICourtRepository courts,
    IVenueRepository venues,
    IUserRepository users,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IPaymentGatewayCredentialsResolver credentials,
    PaymentSettings settings,
    ReservationService reservationService,
    IClock clock)
{
    /// <summary>Abre un partido: crea la reserva del organizador y la publica con cupos.</summary>
    public MatchOutput Open(string organizerId, OpenMatchInput input)
    {
        var user = users.GetById(organizerId) ?? throw new NotFoundError();

        var reservation = reservationService.Create(
            organizerId,
            new CreateReservationInput(input.CourtId, input.Date, input.StartTime, input.EndTime, input.PaymentMethod));

        var reservationEntity = reservations.GetById(reservation.Id) ?? throw new NotFoundError();

        var match = new Match(
            Guid.NewGuid().ToString(),
            reservation.Id,
            organizerId,
            input.MaxPlayers,
            input.Split,
            reservation.TotalPrice,
            string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            reservationEntity.StartDateTime,
            clock.Now);
        match.Join(organizerId, user.Name, clock.Now);
        matches.Add(match);

        return BuildOutput(match);
    }

    /// <summary>Inscribe al usuario autenticado en un partido.</summary>
    public MatchOutput Join(string userId, string matchId)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();
        var user = users.GetById(userId) ?? throw new NotFoundError();

        match.Join(userId, user.Name, clock.Now);
        matches.Update(match);
        return BuildOutput(match);
    }

    /// <summary>Quita al usuario de un partido; si ya había pagado su parte, la reembolsa (FR-018).</summary>
    public async Task<MatchOutput> LeaveAsync(string userId, string matchId)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();

        var payment = payments.GetByMatchAndPayer(matchId, userId);
        if (payment is not null && payment.Status == PaymentStatus.Paid)
            await RefundShareAsync(match, payment);

        match.Leave(userId);
        matches.Update(match);
        return BuildOutput(match);
    }

    /// <summary>Inicia el pago de la parte del usuario en un partido con pago dividido.</summary>
    public async Task<PaymentInitiationOutput> PayShareAsync(string userId, string matchId, PayInput input)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();
        var player = match.PlayerOf(userId);
        if (!match.SplitEnabled)
            throw new ValidationError("Este partido no tiene pago dividido.");

        var existing = payments.GetByMatchAndPayer(matchId, userId);
        if (existing is not null)
        {
            if (existing.Status == PaymentStatus.Paid)
                throw new InvalidPaymentTransitionError("Ya pagaste tu parte de este partido.");
            if (existing.Status == PaymentStatus.Processing)
                return Initiation(existing); // idempotente: reutiliza el checkout en curso
        }

        var method = Parsing.ParsePaymentMethod(input.Method);
        var reservation = reservations.GetById(match.ReservationId) ?? throw new NotFoundError();
        var venue = VenueOf(reservation) ?? throw new VenueNotFoundError();
        var email = users.GetById(userId)?.Email;
        var expiresAt = clock.Now.AddMinutes(settings.ExpiryMinutes);

        var payment = new Payment(
            Guid.NewGuid().ToString(),
            match.ReservationId,
            player.ShareAmount,
            method,
            PaymentStatus.Pending,
            null,
            clock.Now,
            matchId: matchId,
            payerUserId: userId);
        payments.Add(payment);

        GatewayTransactionResult tx;
        try
        {
            tx = await gateway.CreateTransactionAsync(new CreateTransactionRequest(
                payment.Id, payment.Id, payment.Amount, method, email, input.ReturnUrl, credentials.Resolve(venue)));
        }
        catch (PaymentGatewayError)
        {
            payment.Fail("gateway_error_on_create");
            payments.Update(payment);
            throw;
        }

        payment.StartProcessing(tx.TransactionId, tx.CheckoutUrl, expiresAt, method);
        payments.Update(payment);

        match.AttachSharePayment(userId, payment.Id);
        matches.Update(match);

        return Initiation(payment);
    }

    /// <summary>Devuelve el detalle de un partido.</summary>
    public MatchOutput GetDetail(string matchId)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();
        return BuildOutput(match);
    }

    /// <summary>Lista los partidos activos y futuros, opcionalmente por ciudad.</summary>
    public IReadOnlyList<MatchOutput> ListActive(string? city)
    {
        var now = clock.Now;
        var result = new List<MatchOutput>();

        foreach (var match in matches.GetActive())
        {
            var reservation = reservations.GetById(match.ReservationId);
            if (reservation is null || reservation.StartDateTime < now)
                continue;

            var court = courts.GetById(reservation.CourtId);
            var venue = court is null ? null : venues.GetById(court.VenueId);

            if (!string.IsNullOrWhiteSpace(city)
                && venue?.City.Contains(city.Trim(), StringComparison.OrdinalIgnoreCase) != true)
            {
                continue;
            }

            result.Add(BuildOutput(match, reservation, court, venue));
        }

        return result
            .OrderBy(m => m.Date)
            .ThenBy(m => m.StartTime)
            .ToList();
    }

    private async Task RefundShareAsync(Match match, Payment payment)
    {
        var reservation = reservations.GetById(match.ReservationId);
        var venue = reservation is null ? null : VenueOf(reservation);

        payment.RequestRefund();
        payments.Update(payment);

        if (venue is not null)
        {
            try
            {
                var result = await gateway.RefundAsync(
                    payment.GatewayTransactionId ?? payment.Id, payment.Amount, credentials.Resolve(venue));
                payment.ConfirmRefund(result.RefundReference);
                payments.Update(payment);
            }
            catch (Exception)
            {
                // El reembolso queda solicitado; se reconciliará por el webhook de reembolso.
            }
        }
    }

    private Venue? VenueOf(Reservation reservation)
    {
        var court = courts.GetById(reservation.CourtId);
        return court is null ? null : venues.GetById(court.VenueId);
    }

    private PaymentInitiationOutput Initiation(Payment payment) =>
        new(payment.Id, payment.ReservationId, payment.Status.ToString(), payment.Amount,
            payment.Method.ToString(), payment.CheckoutUrl,
            payment.ExpiresAt?.ToString("s"));

    private MatchOutput BuildOutput(Match match)
    {
        var reservation = reservations.GetById(match.ReservationId);
        var court = reservation is null ? null : courts.GetById(reservation.CourtId);
        var venue = court is null ? null : venues.GetById(court.VenueId);
        return BuildOutput(match, reservation, court, venue);
    }

    private static MatchOutput BuildOutput(Match match, Reservation? reservation, Court? court, Venue? venue) =>
        new(
            match.Id,
            match.ReservationId,
            match.OrganizerId,
            court?.VenueId ?? string.Empty,
            venue?.Name ?? string.Empty,
            venue?.City ?? string.Empty,
            court?.Name ?? string.Empty,
            court?.Type.ToString() ?? string.Empty,
            reservation is null ? string.Empty : Mappers.Date(reservation.Date),
            reservation is null ? string.Empty : Mappers.Time(reservation.StartTime),
            reservation is null ? string.Empty : Mappers.Time(reservation.EndTime),
            match.TotalPrice,
            match.MaxPlayers,
            match.SpotsLeft,
            match.SplitEnabled,
            match.PricePerPlayer,
            match.AmountCollected,
            match.Status.ToString(),
            match.Notes,
            match.Players.Select(p => new MatchPlayerOutput(p.UserId, p.Name, p.HasPaid)).ToList());
}
