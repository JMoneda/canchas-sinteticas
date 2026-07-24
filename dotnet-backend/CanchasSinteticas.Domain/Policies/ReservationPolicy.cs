namespace CanchasSinteticas.Domain.Policies;

/// <summary>
/// Reglas de negocio de reservas compartidas por toda la aplicación. Fuente única
/// de verdad para evitar que la disponibilidad y la creación de reservas diverjan.
/// </summary>
public static class ReservationPolicy
{
    /// <summary>Anticipación mínima (en minutos) para poder reservar un turno.</summary>
    public const int MinAdvanceMinutes = 60;

    /// <summary>Máximo de reservas activas simultáneas por cliente.</summary>
    public const int MaxActivePerClient = 3;

    /// <summary>Ventana de cancelación por defecto (en horas) si la sede no define una.</summary>
    public const int DefaultCancellationWindowHours = 3;
}
