namespace CanchasSinteticas.Application.DTOs;

/// <summary>Representación de una cancha.</summary>
public record CourtOutput(
    string Id,
    string VenueId,
    string Name,
    string Type,
    string Surface,
    bool Covered,
    int SlotDurationMinutes,
    bool Active,
    IReadOnlyList<PriceRuleOutput> Prices);

/// <summary>Datos para crear una cancha.</summary>
public record CreateCourtInput(
    string Name,
    string Type,
    string Surface,
    bool Covered,
    int SlotDurationMinutes);

/// <summary>Datos para actualizar una cancha.</summary>
public record UpdateCourtInput(
    string Name,
    string Type,
    string Surface,
    bool Covered,
    int SlotDurationMinutes,
    bool Active);

/// <summary>Regla de precio de entrada.</summary>
public record PriceRuleInput(
    string? DayOfWeek,
    string StartTime,
    string EndTime,
    decimal PricePerHour,
    string Kind);

/// <summary>Regla de precio de salida.</summary>
public record PriceRuleOutput(
    string Id,
    string? DayOfWeek,
    string StartTime,
    string EndTime,
    decimal PricePerHour,
    string Kind);

/// <summary>Conjunto de reglas de precio a establecer para una cancha.</summary>
public record SetPricesInput(IReadOnlyList<PriceRuleInput> Rules);
