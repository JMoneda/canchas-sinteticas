using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Application.Common;

/// <summary>
/// Conversión de entidades de dominio a DTOs de salida.
/// </summary>
public static class Mappers
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH\\:mm";

    /// <summary>Formatea una hora como HH:mm.</summary>
    public static string Time(TimeOnly time) => time.ToString(TimeFormat);

    /// <summary>Formatea una fecha como yyyy-MM-dd.</summary>
    public static string Date(DateOnly date) => date.ToString(DateFormat);

    /// <summary>Mapea un usuario a su representación pública.</summary>
    public static UserOutput ToOutput(User user) =>
        new(user.Id, user.Name, user.Email, user.Phone, user.Role.ToString());

    /// <summary>Mapea una regla de precio.</summary>
    public static PriceRuleOutput ToOutput(PriceRule rule) =>
        new(
            rule.Id,
            rule.DayOfWeek?.ToString(),
            Time(rule.StartTime),
            Time(rule.EndTime),
            rule.PricePerHour,
            rule.Kind);

    /// <summary>Mapea un bloqueo.</summary>
    public static BlackoutOutput ToOutput(Blackout blackout) =>
        new(
            blackout.Id,
            blackout.CourtId,
            Date(blackout.Date),
            Time(blackout.StartTime),
            Time(blackout.EndTime),
            blackout.Reason);

    /// <summary>Mapea una cancha con sus reglas de precio.</summary>
    public static CourtOutput ToOutput(Court court, IReadOnlyList<PriceRule> prices) =>
        new(
            court.Id,
            court.VenueId,
            court.Name,
            court.Type.ToString(),
            court.Surface,
            court.Covered,
            court.SlotDurationMinutes,
            court.Active,
            prices.Select(ToOutput).ToList());

    /// <summary>Mapea una cancha a su resumen con precio "desde".</summary>
    public static CourtSummaryOutput ToSummary(Court court, decimal? minPrice) =>
        new(
            court.Id,
            court.Name,
            court.Type.ToString(),
            court.Surface,
            court.Covered,
            court.SlotDurationMinutes,
            minPrice);
}
