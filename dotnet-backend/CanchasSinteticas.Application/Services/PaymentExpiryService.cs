using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Expira los pagos pendientes cuyo plazo venció y libera la franja de la reserva asociada, para que
/// otros clientes puedan reservarla (FR-008/SC-005). Es invocado periódicamente por un servicio en
/// segundo plano y también es directamente testeable.
/// </summary>
public class PaymentExpiryService(
    IPaymentRepository payments,
    IReservationRepository reservations,
    IClock clock)
{
    /// <summary>Expira los pagos vencidos y libera sus franjas. Devuelve cuántos pagos expiró.</summary>
    public int SweepExpired()
    {
        var now = clock.Now;
        var count = 0;

        foreach (var payment in payments.GetExpirable(now))
        {
            payment.MarkExpired();
            payments.Update(payment);

            if (!payment.IsShare)
            {
                var reservation = reservations.GetById(payment.ReservationId);
                if (reservation is not null && reservation.Status == ReservationStatus.Pending)
                {
                    reservation.Cancel(isLate: false);
                    reservations.Update(reservation);
                }
            }

            count++;
        }

        return count;
    }
}
