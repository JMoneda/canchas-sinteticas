using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de comprobantes en memoria.</summary>
public class InMemoryReceiptRepository(InMemoryDatabase db) : IReceiptRepository
{
    /// <inheritdoc/>
    public void Add(Receipt receipt) => db.Receipts[receipt.Id] = receipt;

    /// <inheritdoc/>
    public Receipt? GetById(string id) => db.Receipts.GetValueOrDefault(id);

    /// <inheritdoc/>
    public Receipt? GetByPayment(string paymentId) =>
        db.Receipts.Values.FirstOrDefault(r => r.PaymentId == paymentId);

    /// <inheritdoc/>
    public Receipt? GetByReservation(string reservationId) =>
        db.Receipts.Values.FirstOrDefault(r => r.ReservationId == reservationId && r.MatchId is null);

    /// <inheritdoc/>
    public Receipt? GetByMatchAndPayer(string matchId, string payerUserId) =>
        db.Receipts.Values.FirstOrDefault(r => r.MatchId == matchId && r.PayerUserId == payerUserId);
}
