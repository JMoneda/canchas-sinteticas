using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de partidos abiertos (matchmaking): abrir un partido a partir de
/// una reserva, unirse, salir, pagar la parte (split payment) y listar los partidos.
/// </summary>
public class MatchService(
    IMatchRepository matches,
    IReservationRepository reservations,
    ICourtRepository courts,
    IVenueRepository venues,
    IUserRepository users,
    IPaymentRepository payments,
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

        var pricePerPlayer = input.Split && input.MaxPlayers > 0
            ? Math.Round(reservation.TotalPrice / input.MaxPlayers, MidpointRounding.AwayFromZero)
            : 0m;

        var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        var match = new Match(
            Guid.NewGuid().ToString(),
            reservation.Id,
            organizerId,
            input.MaxPlayers,
            input.Split,
            pricePerPlayer,
            notes,
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

    /// <summary>Quita al usuario autenticado de un partido.</summary>
    public MatchOutput Leave(string userId, string matchId)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();
        match.Leave(userId);
        matches.Update(match);
        return BuildOutput(match);
    }

    /// <summary>Paga (simulado) la parte del usuario autenticado en un partido con split.</summary>
    public MatchOutput PayShare(string userId, string matchId)
    {
        var match = matches.GetById(matchId) ?? throw new NotFoundError();
        match.PayShare(userId);
        matches.Update(match);

        SettleReservationIfCovered(match);
        return BuildOutput(match);
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

    // Cuando las partes cubren el total de la reserva, se marca el pago de la reserva como realizado.
    private void SettleReservationIfCovered(Match match)
    {
        var reservation = reservations.GetById(match.ReservationId);
        var payment = payments.GetByReservation(match.ReservationId);
        if (reservation is null || payment is null || payment.Status == PaymentStatus.Paid)
            return;

        if (match.AmountCollected >= reservation.TotalPrice)
        {
            payment.MarkPaid($"SPLIT-{match.Id[..8].ToUpperInvariant()}");
            payments.Update(payment);
        }
    }

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
            reservation?.TotalPrice ?? 0m,
            match.MaxPlayers,
            match.SpotsLeft,
            match.SplitEnabled,
            match.PricePerPlayer,
            match.AmountCollected,
            match.Status.ToString(),
            match.Notes,
            match.Players.Select(p => new MatchPlayerOutput(p.UserId, p.Name, p.HasPaid)).ToList());
}
