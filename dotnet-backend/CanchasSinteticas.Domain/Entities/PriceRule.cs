namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Regla de precio por franja horaria de una cancha. Permite tarifas distintas
/// según el día de la semana y la hora (diurno, nocturno, fin de semana, festivo).
/// </summary>
public class PriceRule
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Cancha a la que aplica.</summary>
    public string CourtId { get; }

    /// <summary>Día de la semana al que aplica; <c>null</c> significa cualquier día.</summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>Inicio de la franja horaria (inclusive).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Fin de la franja horaria (exclusive).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Precio por hora dentro de la franja.</summary>
    public decimal PricePerHour { get; set; }

    /// <summary>Etiqueta de la franja (normal, nocturno, festivo...).</summary>
    public string Kind { get; set; }

    /// <summary>Crea una regla de precio.</summary>
    public PriceRule(
        string id,
        string courtId,
        DayOfWeek? dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal pricePerHour,
        string kind)
    {
        Id = id;
        CourtId = courtId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        PricePerHour = pricePerHour;
        Kind = kind;
    }

    /// <summary>Indica si la regla aplica a un instante (fecha + hora) concreto.</summary>
    public bool AppliesTo(DateOnly date, TimeOnly time) =>
        (DayOfWeek is null || DayOfWeek == date.DayOfWeek)
        && time >= StartTime
        && time < EndTime;
}
