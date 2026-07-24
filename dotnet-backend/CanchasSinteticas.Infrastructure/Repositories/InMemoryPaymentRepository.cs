using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de pagos en memoria.</summary>
public class InMemoryPaymentRepository(InMemoryDatabase db) : IPaymentRepository
{
    // Los pagos se indexan por ReservationId (relación 1:1), así el lookup es O(1).

    /// <inheritdoc/>
    public Payment? GetByReservation(string reservationId) =>
        db.Payments.GetValueOrDefault(reservationId);

    /// <inheritdoc/>
    public void Add(Payment payment) => db.Payments[payment.ReservationId] = payment;

    /// <inheritdoc/>
    public void Update(Payment payment) => db.Payments[payment.ReservationId] = payment;
}
