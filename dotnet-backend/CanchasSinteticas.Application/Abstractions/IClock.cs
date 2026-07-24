namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Abstracción del reloj del sistema, para permitir pruebas deterministas.
/// </summary>
public interface IClock
{
    /// <summary>Fecha y hora actuales.</summary>
    DateTime Now { get; }
}
