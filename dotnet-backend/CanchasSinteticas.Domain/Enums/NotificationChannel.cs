namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Canal por el que se notifica al cliente el resultado de pagos y reembolsos.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Dentro de la aplicación.</summary>
    InApp,

    /// <summary>Correo electrónico.</summary>
    Email,

    /// <summary>Mensajería (WhatsApp / SMS).</summary>
    WhatsAppSms,
}
