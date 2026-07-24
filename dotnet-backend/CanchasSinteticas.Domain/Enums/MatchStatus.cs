namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Estado de un partido abierto (matchmaking).
/// </summary>
public enum MatchStatus
{
    /// <summary>Con cupos disponibles para unirse.</summary>
    Open,

    /// <summary>Cupos completos.</summary>
    Full,

    /// <summary>Cancelado (por ejemplo, si se cancela la reserva).</summary>
    Cancelled,

    /// <summary>Ya jugado.</summary>
    Completed,
}
