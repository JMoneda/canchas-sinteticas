using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Pago asociado a una reserva completa o a una parte de un pago dividido (split).
/// El estado solo pasa a <see cref="PaymentStatus.Paid"/> tras la confirmación verificada del
/// proveedor (Regla de Dominio 7); nunca de forma optimista.
/// </summary>
public class Payment
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Reserva a la que corresponde el pago.</summary>
    public string ReservationId { get; }

    /// <summary>Partido asociado cuando el pago es una parte de pago dividido; null si es de reserva.</summary>
    public string? MatchId { get; }

    /// <summary>Jugador que paga la parte (split); null en el pago de la reserva completa.</summary>
    public string? PayerUserId { get; }

    /// <summary>Monto del pago.</summary>
    public decimal Amount { get; }

    /// <summary>Medio de pago elegido.</summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>Estado del pago.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Identificador de la transacción en el proveedor.</summary>
    public string? GatewayTransactionId { get; private set; }

    /// <summary>Referencia/comprobante devuelto por el proveedor al aprobar.</summary>
    public string? GatewayReference { get; private set; }

    /// <summary>Estado crudo informado por el proveedor (auditoría).</summary>
    public string? GatewayStatusRaw { get; private set; }

    /// <summary>URL/token de checkout para redirigir al cliente.</summary>
    public string? CheckoutUrl { get; private set; }

    /// <summary>Referencia del reembolso en el proveedor.</summary>
    public string? RefundReference { get; private set; }

    /// <summary>Fecha de creación.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Fecha de aprobación del pago.</summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>Límite para completar el pago; al superarse se expira.</summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>Crea un pago en el estado inicial indicado.</summary>
    public Payment(
        string id,
        string reservationId,
        decimal amount,
        PaymentMethod method,
        PaymentStatus status,
        string? reference,
        DateTime createdAt,
        string? matchId = null,
        string? payerUserId = null)
    {
        if (amount <= 0)
            throw new ValidationError("El monto del pago debe ser mayor que cero.");
        if ((matchId is null) != (payerUserId is null))
            throw new ValidationError("Un pago de parte debe indicar tanto el partido como el jugador.");

        Id = id;
        ReservationId = reservationId;
        Amount = amount;
        Method = method;
        Status = status;
        GatewayReference = reference;
        CreatedAt = createdAt;
        MatchId = matchId;
        PayerUserId = payerUserId;
    }

    /// <summary>Indica si el pago corresponde a una parte de pago dividido.</summary>
    public bool IsShare => MatchId is not null;

    /// <summary>Crea la transacción en el proveedor: el pago pasa a <see cref="PaymentStatus.Processing"/>.</summary>
    public void StartProcessing(string transactionId, string? checkoutUrl, DateTime expiresAt, PaymentMethod method)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentTransitionError($"No se puede iniciar el cobro desde el estado {Status}.");

        Method = method;
        GatewayTransactionId = transactionId;
        CheckoutUrl = checkoutUrl;
        ExpiresAt = expiresAt;
        Status = PaymentStatus.Processing;
    }

    /// <summary>Marca el pago como aprobado tras la confirmación del proveedor (Regla 7). Idempotente.</summary>
    public void MarkApproved(string transactionId, string reference, DateTime paidAt)
    {
        if (Status == PaymentStatus.Paid)
            return; // idempotente: confirmación repetida no reaplica efectos

        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentTransitionError($"No se puede aprobar un pago en estado {Status}.");

        GatewayTransactionId = transactionId;
        GatewayReference = reference;
        PaidAt = paidAt;
        Status = PaymentStatus.Paid;
    }

    /// <summary>Marca el pago como rechazado por el proveedor. Idempotente.</summary>
    public void MarkRejected(string? raw)
    {
        if (Status == PaymentStatus.Rejected)
            return;

        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentTransitionError($"No se puede rechazar un pago en estado {Status}.");

        GatewayStatusRaw = raw;
        Status = PaymentStatus.Rejected;
    }

    /// <summary>Marca el pago como expirado al superar el plazo. Idempotente.</summary>
    public void MarkExpired()
    {
        if (Status == PaymentStatus.Expired)
            return;

        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentTransitionError($"No se puede expirar un pago en estado {Status}.");

        Status = PaymentStatus.Expired;
    }

    /// <summary>Reactiva un pago expirado para permitir reconfirmar una aprobación tardía.</summary>
    public void Reactivate()
    {
        if (Status != PaymentStatus.Expired)
            throw new InvalidPaymentTransitionError($"Solo se puede reactivar un pago expirado (estado actual {Status}).");

        Status = PaymentStatus.Processing;
    }

    /// <summary>Solicita el reembolso al proveedor. Idempotente si ya está solicitado.</summary>
    public void RequestRefund()
    {
        if (Status == PaymentStatus.RefundRequested)
            return;

        if (Status != PaymentStatus.Paid)
            throw new InvalidPaymentTransitionError($"Solo se puede reembolsar un pago pagado (estado actual {Status}).");

        Status = PaymentStatus.RefundRequested;
    }

    /// <summary>Confirma el reembolso informado por el proveedor. Idempotente.</summary>
    public void ConfirmRefund(string refundReference)
    {
        if (Status == PaymentStatus.Refunded)
            return;

        if (Status != PaymentStatus.RefundRequested)
            throw new InvalidPaymentTransitionError($"No hay un reembolso solicitado que confirmar (estado actual {Status}).");

        RefundReference = refundReference;
        Status = PaymentStatus.Refunded;
    }

    /// <summary>Revierte a pagado cuando el proveedor rechaza/no ejecuta el reembolso.</summary>
    public void FailRefund()
    {
        if (Status != PaymentStatus.RefundRequested)
            throw new InvalidPaymentTransitionError($"No hay un reembolso solicitado que revertir (estado actual {Status}).");

        Status = PaymentStatus.Paid;
    }

    /// <summary>Marca el pago como fallido por un error técnico/de comunicación con el proveedor.</summary>
    public void Fail(string? raw)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentTransitionError($"No se puede fallar un pago en estado {Status}.");

        GatewayStatusRaw = raw;
        Status = PaymentStatus.Failed;
    }
}
