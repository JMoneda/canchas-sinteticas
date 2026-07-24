namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Envía al cliente el resultado de pagos y reembolsos por los canales configurados
/// (aplicación, correo, WhatsApp/SMS).
/// </summary>
public interface INotificationSender
{
    /// <summary>Notifica un evento de pago/reembolso al usuario indicado.</summary>
    Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>Tipo de evento a notificar.</summary>
public enum PaymentNotificationKind
{
    /// <summary>Pago aprobado.</summary>
    Approved,

    /// <summary>Pago rechazado.</summary>
    Rejected,

    /// <summary>Reembolso confirmado.</summary>
    Refunded,
}

/// <summary>Datos de una notificación de pago/reembolso.</summary>
public record PaymentNotification(
    string UserId,
    PaymentNotificationKind Kind,
    string ReservationId,
    decimal Amount,
    string? ReceiptReference);
