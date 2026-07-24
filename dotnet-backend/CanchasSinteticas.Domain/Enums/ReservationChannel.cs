namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Canal por el que se originó una reserva.
/// </summary>
public enum ReservationChannel
{
    /// <summary>Reserva creada por el cliente desde el marketplace.</summary>
    Online,

    /// <summary>Reserva creada manualmente por el dueño/staff (walk-in, teléfono).</summary>
    Manual,
}
