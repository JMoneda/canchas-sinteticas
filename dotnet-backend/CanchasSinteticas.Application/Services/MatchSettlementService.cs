using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Aplica la política de expiración del recaudo de los partidos con pago dividido (FR-017): si al
/// llegar la fecha límite el recaudo no cubre el total, reembolsa las partes ya pagadas, cancela el
/// partido y libera la reserva.
/// </summary>
public class MatchSettlementService(
    IMatchRepository matches,
    IReservationRepository reservations,
    IPaymentRepository payments,
    ICourtRepository courts,
    IVenueRepository venues,
    IPaymentGateway gateway,
    IPaymentGatewayCredentialsResolver credentials,
    IClock clock)
{
    /// <summary>Expira los recaudos vencidos e incompletos. Devuelve cuántos partidos liquidó.</summary>
    public async Task<int> SweepAsync()
    {
        var now = clock.Now;
        var count = 0;

        foreach (var match in matches.GetActive())
        {
            if (!match.SplitEnabled || match.IsFullyCollected || now < match.SettlementDeadline)
                continue;

            foreach (var player in match.PaidPlayers)
            {
                var payment = payments.GetByMatchAndPayer(match.Id, player.UserId);
                if (payment is not null && payment.Status == PaymentStatus.Paid)
                    await RefundAsync(match, payment);
            }

            match.Cancel();
            matches.Update(match);

            var reservation = reservations.GetById(match.ReservationId);
            if (reservation is not null && reservation.Status == ReservationStatus.Pending)
            {
                reservation.Cancel(isLate: false);
                reservations.Update(reservation);
            }

            count++;
        }

        return count;
    }

    private async Task RefundAsync(Match match, Payment payment)
    {
        var reservation = reservations.GetById(match.ReservationId);
        var court = reservation is null ? null : courts.GetById(reservation.CourtId);
        var venue = court is null ? null : venues.GetById(court.VenueId);

        payment.RequestRefund();
        payments.Update(payment);
        if (venue is null)
            return;

        try
        {
            var result = await gateway.RefundAsync(
                payment.GatewayTransactionId ?? payment.Id, payment.Amount, credentials.Resolve(venue));
            payment.ConfirmRefund(result.RefundReference);
            payments.Update(payment);
        }
        catch (Exception)
        {
            // Queda solicitado; se reconciliará por el webhook de reembolso.
        }
    }
}
