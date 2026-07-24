namespace CanchasSinteticas.Infrastructure.Payments;

/// <summary>Configuración de la sección <c>Payments</c> de appsettings.</summary>
public class PaymentsOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Payments";

    /// <summary>Proveedor activo (por ahora "Wompi").</summary>
    public string Provider { get; set; } = "Wompi";

    /// <summary>Minutos que una transacción puede estar pendiente antes de expirar.</summary>
    public int ExpiryMinutes { get; set; } = 15;

    /// <summary>Credenciales/endpoint de Wompi (plataforma / marketplace).</summary>
    public WompiOptions Wompi { get; set; } = new();

    /// <summary>Configuración de canales de notificación.</summary>
    public NotificationsOptions Notifications { get; set; } = new();
}

/// <summary>Credenciales y endpoint de Wompi.</summary>
public class WompiOptions
{
    /// <summary>URL base de la API (sandbox o producción).</summary>
    public string BaseUrl { get; set; } = "https://sandbox.wompi.co/v1";

    /// <summary>Llave pública.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Llave privada.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>Secreto de integridad (firma de checkout).</summary>
    public string IntegritySecret { get; set; } = string.Empty;

    /// <summary>Secreto de eventos (verificación del webhook).</summary>
    public string EventsSecret { get; set; } = string.Empty;
}

/// <summary>Activación de canales de notificación.</summary>
public class NotificationsOptions
{
    /// <summary>Correo electrónico.</summary>
    public ChannelOptions Email { get; set; } = new();

    /// <summary>Mensajería WhatsApp/SMS.</summary>
    public ChannelOptions WhatsAppSms { get; set; } = new();
}

/// <summary>Activación de un canal.</summary>
public class ChannelOptions
{
    /// <summary>Indica si el canal está habilitado.</summary>
    public bool Enabled { get; set; }
}
