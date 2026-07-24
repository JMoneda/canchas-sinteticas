namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para abrir un partido (crea la reserva y lo publica con cupos).</summary>
public record OpenMatchInput(
    string CourtId,
    string Date,
    string StartTime,
    string EndTime,
    int MaxPlayers,
    bool Split,
    string? Notes,
    string? PaymentMethod);

/// <summary>Jugador inscrito en un partido.</summary>
public record MatchPlayerOutput(string UserId, string Name, bool HasPaid);

/// <summary>Representación de un partido abierto.</summary>
public record MatchOutput(
    string Id,
    string ReservationId,
    string OrganizerId,
    string VenueId,
    string VenueName,
    string City,
    string CourtName,
    string CourtType,
    string Date,
    string StartTime,
    string EndTime,
    decimal TotalPrice,
    int MaxPlayers,
    int SpotsLeft,
    bool SplitEnabled,
    decimal PricePerPlayer,
    decimal AmountCollected,
    string Status,
    string? Notes,
    IReadOnlyList<MatchPlayerOutput> Players);
