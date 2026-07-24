namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Verifica la autenticidad del cuerpo de un webhook del proveedor y lo traduce a un evento
/// normalizado independiente del proveedor. Devuelve null si la firma no se puede verificar.
/// </summary>
public interface IPaymentWebhookVerifier
{
    /// <summary>Verifica y parsea el cuerpo del webhook; null si no es auténtico o no es interpretable.</summary>
    PaymentWebhookEvent? VerifyAndParse(string rawBody);
}

/// <summary>Estado normalizado de una transacción reportado por el proveedor.</summary>
public enum PaymentWebhookStatus
{
    /// <summary>Aprobada.</summary>
    Approved,

    /// <summary>Rechazada.</summary>
    Declined,

    /// <summary>Anulada / reembolsada.</summary>
    Voided,

    /// <summary>En proceso / pendiente.</summary>
    Pending,

    /// <summary>Error del proveedor.</summary>
    Error,
}

/// <summary>Evento de webhook normalizado.</summary>
public record PaymentWebhookEvent(
    string EventId,
    string TransactionId,
    string Reference,
    PaymentWebhookStatus Status,
    string RawStatus);
