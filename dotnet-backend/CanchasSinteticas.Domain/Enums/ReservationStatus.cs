namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida de una reserva.
/// </summary>
public enum ReservationStatus
{
    /// <summary>Reserva pendiente de pago: retiene la franja pero aún no está confirmada.</summary>
    Pending,

    /// <summary>Reserva confirmada y vigente.</summary>
    Confirmed,

    /// <summary>Reserva cancelada dentro de la política permitida.</summary>
    Cancelled,

    /// <summary>Reserva ya jugada/completada.</summary>
    Completed,

    /// <summary>El cliente no se presentó o canceló fuera de plazo.</summary>
    NoShow,
}
