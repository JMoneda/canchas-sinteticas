using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de pagos en memoria, indexado por identificador de pago.</summary>
public class InMemoryPaymentRepository(InMemoryDatabase db) : IPaymentRepository
{
    // Se indexa por Payment.Id para soportar múltiples pagos por reserva (partes de split)
    // y la búsqueda por transacción del proveedor.

    /// <inheritdoc/>
    public Payment? GetById(string id) => db.Payments.GetValueOrDefault(id);

    /// <inheritdoc/>
    public Payment? GetByReservation(string reservationId) =>
        db.Payments.Values.FirstOrDefault(p => p.ReservationId == reservationId && p.MatchId is null);

    /// <inheritdoc/>
    public Payment? GetByGatewayTransactionId(string gatewayTransactionId) =>
        db.Payments.Values.FirstOrDefault(p => p.GatewayTransactionId == gatewayTransactionId);

    /// <inheritdoc/>
    public Payment? GetByMatchAndPayer(string matchId, string payerUserId) =>
        db.Payments.Values.FirstOrDefault(p => p.MatchId == matchId && p.PayerUserId == payerUserId);

    /// <inheritdoc/>
    public IReadOnlyList<Payment> GetSharesByMatch(string matchId) =>
        db.Payments.Values.Where(p => p.MatchId == matchId).ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Payment> GetExpirable(DateTime now) =>
        db.Payments.Values
            .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing)
                && p.ExpiresAt is not null
                && p.ExpiresAt <= now)
            .ToList();

    /// <inheritdoc/>
    public void Add(Payment payment) => db.Payments[payment.Id] = payment;

    /// <inheritdoc/>
    public void Update(Payment payment) => db.Payments[payment.Id] = payment;
}
