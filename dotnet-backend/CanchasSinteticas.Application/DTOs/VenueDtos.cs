namespace CanchasSinteticas.Application.DTOs;

/// <summary>Resumen de una sede para el listado del marketplace.</summary>
public record VenueSummaryOutput(
    string Id,
    string Name,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    string? Phone,
    IReadOnlyList<string> Photos,
    IReadOnlyList<string> Services,
    decimal? MinPrice,
    int CourtCount);

/// <summary>Detalle completo de una sede con sus canchas.</summary>
public record VenueDetailOutput(
    string Id,
    string OwnerId,
    string Name,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    string? Phone,
    IReadOnlyList<string> Photos,
    IReadOnlyList<string> Services,
    string OpeningTime,
    string ClosingTime,
    int CancellationWindowHours,
    bool Active,
    IReadOnlyList<CourtSummaryOutput> Courts);

/// <summary>Resumen de una cancha dentro de una sede.</summary>
public record CourtSummaryOutput(
    string Id,
    string Name,
    string Type,
    string Surface,
    bool Covered,
    int SlotDurationMinutes,
    decimal? MinPrice);

/// <summary>Datos para crear una sede.</summary>
public record CreateVenueInput(
    string Name,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    string? Phone,
    IReadOnlyList<string>? Photos,
    IReadOnlyList<string>? Services,
    string OpeningTime,
    string ClosingTime,
    int CancellationWindowHours);

/// <summary>Datos para actualizar una sede.</summary>
public record UpdateVenueInput(
    string Name,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    string? Phone,
    IReadOnlyList<string>? Photos,
    IReadOnlyList<string>? Services,
    string OpeningTime,
    string ClosingTime,
    int CancellationWindowHours,
    bool Active);
