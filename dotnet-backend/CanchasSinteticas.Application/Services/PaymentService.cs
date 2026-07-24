using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Simulación de pago de una reserva. En producción aquí se integraría una
/// pasarela real (Wompi, PayU, Mercado Pago, ePayco).
/// </summary>
public class PaymentService(
    IReservationRepository reservations,
    IPaymentRepository payments)
{
    /// <summary>Procesa (simula) el pago de una reserva del cliente.</summary>
    public PaymentOutput Pay(string clientId, string reservationId, PayInput input)
    {
        var reservation = reservations.GetById(reservationId) ?? throw new NotFoundError();
        if (reservation.ClientId != clientId)
            throw new NotAuthorizedError();

        var payment = payments.GetByReservation(reservationId) ?? throw new NotFoundError();

        // Validamos el medio recibido aunque el registro conserva el elegido al reservar.
        _ = Parsing.ParsePaymentMethod(input.Method);

        if (payment.Status != PaymentStatus.Paid)
        {
            var reference = $"SIM-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            payment.MarkPaid(reference);
            payments.Update(payment);
        }

        return new PaymentOutput(
            reservationId,
            payment.Amount,
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.Reference);
    }
}
