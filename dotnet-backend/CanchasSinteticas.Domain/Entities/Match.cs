using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Partido abierto (matchmaking): una reserva que su organizador publica con cupos para que otros
/// jugadores se unan. Con pago dividido, cada jugador paga una parte exacta del total.
/// </summary>
public class Match
{
    /// <summary>Cupo máximo permitido para un partido (fútbol 11 + suplentes).</summary>
    public const int MaxAllowedPlayers = 30;

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

    /// <summary>Precio total de la reserva del partido.</summary>
    public decimal TotalPrice { get; }

    /// <summary>Fecha límite para completar el recaudo; si no se cumple, se aplica la política de expiración.</summary>
    public DateTime SettlementDeadline { get; }

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
        decimal totalPrice,
        string? notes,
        DateTime settlementDeadline,
        DateTime createdAt)
    {
        if (maxPlayers < 2)
            throw new ValidationError("El partido debe permitir al menos 2 jugadores.");
        if (maxPlayers > MaxAllowedPlayers)
            throw new ValidationError($"El cupo máximo de un partido es {MaxAllowedPlayers} jugadores.");

        Id = id;
        ReservationId = reservationId;
        OrganizerId = organizerId;
        MaxPlayers = maxPlayers;
        SplitEnabled = splitEnabled;
        TotalPrice = totalPrice;
        Notes = notes;
        SettlementDeadline = settlementDeadline;
        Status = MatchStatus.Open;
        CreatedAt = createdAt;
    }

    /// <summary>Cupos disponibles.</summary>
    public int SpotsLeft => MaxPlayers - players.Count;

    /// <summary>Parte representativa por jugador (para mostrar).</summary>
    public decimal PricePerPlayer => SplitEnabled ? ShareFor(0) : 0m;

    /// <summary>Monto recaudado con las partes ya pagadas.</summary>
    public decimal AmountCollected => players.Where(p => p.HasPaid).Sum(p => p.ShareAmount);

    /// <summary>Indica si el recaudo cubre el total de la reserva.</summary>
    public bool IsFullyCollected => SplitEnabled && AmountCollected >= TotalPrice;

    /// <summary>
    /// Parte exacta que corresponde a la posición <paramref name="index"/> del cupo. El residuo se
    /// reparte de a un peso entre las primeras posiciones, de modo que la suma de las
    /// <see cref="MaxPlayers"/> partes iguale exactamente <see cref="TotalPrice"/> (FR-013).
    /// </summary>
    public decimal ShareFor(int index)
    {
        var baseShare = decimal.Truncate(TotalPrice / MaxPlayers);
        var remainder = (int)(TotalPrice - (baseShare * MaxPlayers));
        return baseShare + (index < remainder ? 1m : 0m);
    }

    /// <summary>Inscribe un jugador en el partido, asignándole su parte exacta.</summary>
    public void Join(string userId, string name, DateTime now)
    {
        if (Status != MatchStatus.Open)
            throw new MatchNotOpenError();
        if (players.Any(p => p.UserId == userId))
            throw new AlreadyJoinedError();
        if (players.Count >= MaxPlayers)
            throw new MatchFullError();

        var share = SplitEnabled ? ShareFor(players.Count) : 0m;
        players.Add(new MatchPlayer(userId, name, share, now));
        if (players.Count >= MaxPlayers)
            Status = MatchStatus.Full;
    }

    /// <summary>Quita a un jugador del partido. El organizador no puede salir.</summary>
    public MatchPlayer Leave(string userId)
    {
        if (userId == OrganizerId)
            throw new OrganizerCannotLeaveError();

        var player = players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new NotJoinedError();

        players.Remove(player);
        if (Status == MatchStatus.Full)
            Status = MatchStatus.Open;

        return player;
    }

    /// <summary>Devuelve el jugador indicado o lanza si no está inscrito.</summary>
    public MatchPlayer PlayerOf(string userId) =>
        players.FirstOrDefault(p => p.UserId == userId) ?? throw new NotJoinedError();

    /// <summary>Enlaza el pago (parte) que un jugador acaba de iniciar.</summary>
    public void AttachSharePayment(string userId, string paymentId)
    {
        EnsureSplit();
        PlayerOf(userId).AttachPayment(paymentId);
    }

    /// <summary>Confirma el pago de la parte de un jugador tras la aprobación del proveedor.</summary>
    public void ConfirmSharePayment(string userId, string paymentId)
    {
        EnsureSplit();
        PlayerOf(userId).MarkPaid(paymentId);
    }

    /// <summary>Jugadores que ya pagaron su parte (para reembolsar al expirar el recaudo).</summary>
    public IReadOnlyList<MatchPlayer> PaidPlayers => players.Where(p => p.HasPaid).ToList();

    /// <summary>Cancela el partido.</summary>
    public void Cancel() => Status = MatchStatus.Cancelled;

    private void EnsureSplit()
    {
        if (!SplitEnabled)
            throw new ValidationError("Este partido no tiene pago dividido.");
    }
}
