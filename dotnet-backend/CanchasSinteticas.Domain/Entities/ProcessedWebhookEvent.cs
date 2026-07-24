namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Registro de un evento de webhook del proveedor ya procesado. Permite aplicar los eventos de
/// forma idempotente: un evento repetido no vuelve a cambiar el estado del pago.
/// </summary>
public class ProcessedWebhookEvent(string eventId, string gatewayTransactionId, DateTime receivedAt)
{
    /// <summary>Identificador del evento en el proveedor (clave de idempotencia).</summary>
    public string EventId { get; } = eventId;

    /// <summary>Transacción del proveedor referida por el evento.</summary>
    public string GatewayTransactionId { get; } = gatewayTransactionId;

    /// <summary>Momento en que se procesó por primera vez.</summary>
    public DateTime ReceivedAt { get; } = receivedAt;
}
