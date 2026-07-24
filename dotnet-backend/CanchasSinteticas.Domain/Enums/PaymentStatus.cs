namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Estado de un pago asociado a una reserva.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Pago pendiente (ej. pagar en sede).</summary>
    Pending,

    /// <summary>Pago realizado con éxito.</summary>
    Paid,

    /// <summary>Pago reembolsado tras una cancelación.</summary>
    Refunded,

    /// <summary>El intento de pago falló.</summary>
    Failed,
}
