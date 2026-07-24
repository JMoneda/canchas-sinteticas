using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Partido abierto (matchmaking): una reserva que su organizador publica con cupos
/// para que otros jugadores se unan.
/// </summary>
public class Match
{
    private readonly List<MatchPlayer> players = [];

    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Reserva sobre la que se juega el partido.</summary>
    public string ReservationId { get; }

    /// <summary>Usuario organizador (dueño de la reserva).</summary>
    public string OrganizerId { get; }

    /// <summary>Cupo total de jugadores.</summary>
    public int MaxPlayers { get; }

    /// <summary>Indica si el costo se divide entre los jugadores (split payment).</summary>
    public bool SplitEnabled { get; }

    /// <summary>Parte que paga cada jugador cuando el split está activo.</summary>
    public decimal PricePerPlayer { get; }

    /// <summary>Nota opcional del organizador (nivel, indicaciones).</summary>
    public string? Notes { get; set; }

    /// <summary>Estado del partido.</summary>
    public MatchStatus Status { get; private set; }

    /// <summary>Fecha de creación.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Jugadores inscritos.</summary>
    public IReadOnlyList<MatchPlayer> Players => players;

    /// <summary>Crea un partido abierto.</summary>
    public Match(
        string id,
        string reservationId,
        string organizerId,
        int maxPlayers,
        bool splitEnabled,
        decimal pricePerPlayer,
        string? notes,
        DateTime createdAt)
    {
        if (maxPlayers < 2)
            throw new ValidationError("El partido debe permitir al menos 2 jugadores.");

        Id = id;
        ReservationId = reservationId;
        OrganizerId = organizerId;
        MaxPlayers = maxPlayers;
        SplitEnabled = splitEnabled;
        PricePerPlayer = pricePerPlayer;
        Notes = notes;
        Status = MatchStatus.Open;
        CreatedAt = createdAt;
    }

    /// <summary>Cupos disponibles.</summary>
    public int SpotsLeft => MaxPlayers - players.Count;

    /// <summary>Monto recaudado con las partes ya pagadas.</summary>
    public decimal AmountCollected => players.Count(p => p.HasPaid) * PricePerPlayer;

    /// <summary>Inscribe un jugador en el partido.</summary>
    public void Join(string userId, string name, DateTime now)
    {
        if (Status != MatchStatus.Open)
            throw new MatchNotOpenError();
        if (players.Any(p => p.UserId == userId))
            throw new AlreadyJoinedError();
        if (players.Count >= MaxPlayers)
            throw new MatchFullError();

        players.Add(new MatchPlayer(userId, name, now));
        if (players.Count >= MaxPlayers)
            Status = MatchStatus.Full;
    }

    /// <summary>Quita a un jugador del partido. El organizador no puede salir.</summary>
    public void Leave(string userId)
    {
        if (userId == OrganizerId)
            throw new OrganizerCannotLeaveError();

        var player = players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new NotJoinedError();

        players.Remove(player);
        if (Status == MatchStatus.Full)
            Status = MatchStatus.Open;
    }

    /// <summary>Marca como pagada la parte del jugador indicado.</summary>
    public void PayShare(string userId)
    {
        if (!SplitEnabled)
            throw new ValidationError("Este partido no tiene pago dividido.");

        var player = players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new NotJoinedError();

        player.MarkPaid();
    }

    /// <summary>Cancela el partido.</summary>
    public void Cancel() => Status = MatchStatus.Cancelled;
}
