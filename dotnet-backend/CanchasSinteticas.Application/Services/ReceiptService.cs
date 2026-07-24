using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Genera y entrega los comprobantes de pago. La generación ocurre al aprobarse un pago; la entrega
/// (PDF o datos) está restringida al titular del pago y al dueño de la sede (FR-019..FR-022).
/// </summary>
public class ReceiptService(
    IReceiptRepository receipts,
    IReservationRepository reservations,
    ICourtRepository courts,
    IVenueRepository venues,
    IUserRepository users,
    IReceiptGenerator generator,
    IClock clock)
{
    /// <summary>Genera (una sola vez) el comprobante de un pago aprobado. Idempotente por pago.</summary>
    public Receipt GenerateFor(Payment payment)
    {
        var existing = receipts.GetByPayment(payment.Id);
        if (existing is not null)
            return existing;

        var reservation = reservations.GetById(payment.ReservationId);
        var court = reservation is null ? null : courts.GetById(reservation.CourtId);
        var venue = court is null ? null : venues.GetById(court.VenueId);

        var payerId = payment.PayerUserId ?? reservation?.ClientId;
        var payerName = (payerId is null ? null : users.GetById(payerId)?.Name)
            ?? reservation?.ClientName
            ?? "Cliente";

        var id = Guid.NewGuid().ToString();
        var receipt = new Receipt(
            id,
            $"REC-{clock.Now:yyyyMMddHHmmss}-{id[..4].ToUpperInvariant()}",
            payment.Id,
            payment.ReservationId,
            payment.MatchId,
            payment.PayerUserId,
            payerName,
            payment.Amount,
            payment.Method.ToString(),
            payment.GatewayReference ?? payment.Id,
            venue?.Name ?? string.Empty,
            court?.Name ?? string.Empty,
            clock.Now);

        receipts.Add(receipt);
        return receipt;
    }

    /// <summary>Devuelve el comprobante (datos + PDF) de una reserva, validando el acceso.</summary>
    public (Receipt Receipt, byte[] Pdf) GetReservationReceipt(string userId, string reservationId)
    {
        var receipt = receipts.GetByReservation(reservationId) ?? throw new NotFoundError();
        Authorize(userId, receipt);
        return (receipt, generator.GeneratePdf(receipt));
    }

    /// <summary>Devuelve el comprobante de la parte de un jugador en un partido, validando el acceso.</summary>
    public (Receipt Receipt, byte[] Pdf) GetShareReceipt(string userId, string matchId)
    {
        var receipt = receipts.GetByMatchAndPayer(matchId, userId) ?? throw new NotFoundError();
        Authorize(userId, receipt);
        return (receipt, generator.GeneratePdf(receipt));
    }

    /// <summary>Mapea el comprobante a su DTO de datos.</summary>
    public static ReceiptOutput ToOutput(Receipt r) =>
        new(r.Number, r.Amount, r.Method, r.GatewayReference, r.IssuedAt.ToString("s"),
            r.VenueName, r.CourtName, r.PayerName);

    private void Authorize(string userId, Receipt receipt)
    {
        var reservation = reservations.GetById(receipt.ReservationId);
        var isClient = receipt.PayerUserId is not null
            ? receipt.PayerUserId == userId
            : reservation?.ClientId == userId;

        var isOwner = false;
        var court = reservation is null ? null : courts.GetById(reservation.CourtId);
        if (court is not null)
        {
            var venue = venues.GetById(court.VenueId);
            isOwner = venue is not null && venue.OwnerId == userId;
        }

        if (!isClient && !isOwner)
            throw new NotAuthorizedError();
    }
}
