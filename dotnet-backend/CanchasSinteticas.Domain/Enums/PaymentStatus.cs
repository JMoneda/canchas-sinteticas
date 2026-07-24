namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Estado de un pago asociado a una reserva o a una parte de pago dividido.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Pago pendiente: creado pero sin intento de cobro confirmado.</summary>
    Pending,

    /// <summary>Transacción creada en el proveedor, esperando confirmación.</summary>
    Processing,

    /// <summary>Aprobado por el proveedor (confirmación verificada).</summary>
    Paid,

    /// <summary>Rechazado por el proveedor.</summary>
    Rejected,

    /// <summary>Venció el plazo sin aprobación.</summary>
    Expired,

    /// <summary>Reembolso solicitado al proveedor, aún sin confirmar.</summary>
    RefundRequested,

    /// <summary>Reembolso confirmado por el proveedor.</summary>
    Refunded,

    /// <summary>El intento de pago falló por un error técnico/de comunicación.</summary>
    Failed,
}
