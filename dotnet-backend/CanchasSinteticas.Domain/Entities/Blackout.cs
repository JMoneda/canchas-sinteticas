namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Bloqueo manual de una cancha (mantenimiento, evento privado, clima) que
/// impide reservar durante la franja indicada.
/// </summary>
public class Blackout
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Cancha bloqueada.</summary>
    public string CourtId { get; }

    /// <summary>Fecha del bloqueo.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Hora de inicio del bloqueo.</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Hora de fin del bloqueo.</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Motivo del bloqueo.</summary>
    public string Reason { get; set; }

    /// <summary>Crea un bloqueo.</summary>
    public Blackout(
        string id,
        string courtId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        string reason)
    {
        Id = id;
        CourtId = courtId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        Reason = reason;
    }

    /// <summary>Indica si el bloqueo cubre (se solapa con) la franja indicada.</summary>
    public bool Covers(DateOnly date, TimeOnly startTime, TimeOnly endTime) =>
        Date == date && StartTime < endTime && startTime < EndTime;
}
