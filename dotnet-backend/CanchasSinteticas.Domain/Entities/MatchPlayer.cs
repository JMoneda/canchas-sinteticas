namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Jugador inscrito en un partido abierto, con el estado de su pago (split payment).
/// </summary>
public class MatchPlayer(string userId, string name, DateTime joinedAt)
{
    /// <summary>Identificador del usuario.</summary>
    public string UserId { get; } = userId;

    /// <summary>Nombre para mostrar.</summary>
    public string Name { get; } = name;

    /// <summary>Momento en que se unió.</summary>
    public DateTime JoinedAt { get; } = joinedAt;

    /// <summary>Indica si el jugador ya pagó su parte.</summary>
    public bool HasPaid { get; private set; }

    /// <summary>Marca la parte del jugador como pagada.</summary>
    public void MarkPaid() => HasPaid = true;
}
