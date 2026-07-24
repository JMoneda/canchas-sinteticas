namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Comprobante inmutable generado al aprobarse un pago. Es un snapshot de los datos de la
/// transacción al momento de emisión (no se recalcula después).
/// </summary>
public class Receipt
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Consecutivo legible del comprobante.</summary>
    public string Number { get; }

    /// <summary>Pago asociado.</summary>
    public string PaymentId { get; }

    /// <summary>Reserva asociada.</summary>
    public string ReservationId { get; }

    /// <summary>Partido asociado si es una parte de pago dividido.</summary>
    public string? MatchId { get; }

    /// <summary>Jugador (split), si aplica.</summary>
    public string? PayerUserId { get; }

    /// <summary>Nombre que figura en el comprobante.</summary>
    public string PayerName { get; }

    /// <summary>Monto.</summary>
    public decimal Amount { get; }

    /// <summary>Método de pago usado.</summary>
    public string Method { get; }

    /// <summary>Referencia del proveedor.</summary>
    public string GatewayReference { get; }

    /// <summary>Sede (snapshot).</summary>
    public string VenueName { get; }

    /// <summary>Cancha (snapshot).</summary>
    public string CourtName { get; }

    /// <summary>Fecha de emisión.</summary>
    public DateTime IssuedAt { get; }

    /// <summary>Crea un comprobante.</summary>
    public Receipt(
        string id,
        string number,
        string paymentId,
        string reservationId,
        string? matchId,
        string? payerUserId,
        string payerName,
        decimal amount,
        string method,
        string gatewayReference,
        string venueName,
        string courtName,
        DateTime issuedAt)
    {
        Id = id;
        Number = number;
        PaymentId = paymentId;
        ReservationId = reservationId;
        MatchId = matchId;
        PayerUserId = payerUserId;
        PayerName = payerName;
        Amount = amount;
        Method = method;
        GatewayReference = gatewayReference;
        VenueName = venueName;
        CourtName = courtName;
        IssuedAt = issuedAt;
    }
}
