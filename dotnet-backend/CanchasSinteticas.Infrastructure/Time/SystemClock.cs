using CanchasSinteticas.Application.Abstractions;

namespace CanchasSinteticas.Infrastructure.Time;

/// <summary>Reloj basado en la hora local del sistema.</summary>
public class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTime Now => DateTime.Now;
}
