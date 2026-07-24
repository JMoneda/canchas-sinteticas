using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.ValueObjects;

/// <summary>
/// Franja de tiempo de una reserva en un día concreto. Value object inmutable.
/// Las reglas de horario de operación y duración de bloque dependen de la sede/cancha
/// y se validan en la capa de aplicación; aquí solo se garantiza un rango coherente.
/// </summary>
public class TimeSlot
{
    /// <summary>Fecha del turno.</summary>
    public DateOnly Date { get; }

    /// <summary>Hora de inicio.</summary>
    public TimeOnly StartTime { get; }

    /// <summary>Hora de fin.</summary>
    public TimeOnly EndTime { get; }

    /// <summary>Crea una franja validando que el rango sea coherente.</summary>
    public TimeSlot(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new InvalidBlockError();

        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>Duración de la franja.</summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>Fecha y hora de inicio combinadas.</summary>
    public DateTime StartDateTime => Date.ToDateTime(StartTime);

    /// <summary>Fecha y hora de fin combinadas.</summary>
    public DateTime EndDateTime => Date.ToDateTime(EndTime);

    /// <summary>Indica si la franja respeta la anticipación mínima requerida.</summary>
    public bool IsBookable(DateTime now, int minAdvanceMinutes) =>
        StartDateTime - now >= TimeSpan.FromMinutes(minAdvanceMinutes);

    /// <summary>Indica si la franja está dentro del horario de operación indicado.</summary>
    public bool WithinOperatingHours(TimeOnly opening, TimeOnly closing) =>
        StartTime >= opening && EndTime <= closing;

    /// <summary>Indica si esta franja se solapa con otra.</summary>
    public bool OverlapsWith(TimeSlot other)
    {
        if (Date != other.Date)
            return false;
        return StartTime < other.EndTime && other.StartTime < EndTime;
    }
}
