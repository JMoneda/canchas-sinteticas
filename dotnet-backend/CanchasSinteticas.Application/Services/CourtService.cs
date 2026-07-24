using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de gestión de canchas y sus tarifas (panel del dueño).
/// </summary>
public class CourtService(
    IVenueRepository venues,
    ICourtRepository courts,
    IPriceRuleRepository prices)
{
    /// <summary>Lista las canchas de una sede del dueño con sus tarifas.</summary>
    public IReadOnlyList<CourtOutput> GetByVenue(string ownerId, string venueId)
    {
        Ownership.OwnedVenue(venues, ownerId, venueId);
        return courts.GetByVenue(venueId)
            .Select(c => Mappers.ToOutput(c, prices.GetByCourt(c.Id)))
            .ToList();
    }

    /// <summary>Crea una cancha en una sede del dueño.</summary>
    public CourtOutput Create(string ownerId, string venueId, CreateCourtInput input)
    {
        Ownership.OwnedVenue(venues, ownerId, venueId);
        ValidateCourt(input.Name, input.SlotDurationMinutes);

        var court = new Court(
            Guid.NewGuid().ToString(),
            venueId,
            input.Name.Trim(),
            Parsing.ParseCourtType(input.Type),
            NormalizeSurface(input.Surface),
            input.Covered,
            input.SlotDurationMinutes,
            true);

        courts.Add(court);
        return Mappers.ToOutput(court, prices.GetByCourt(court.Id));
    }

    /// <summary>Actualiza una cancha del dueño.</summary>
    public CourtOutput Update(string ownerId, string courtId, UpdateCourtInput input)
    {
        var court = Ownership.OwnedCourt(venues, courts, ownerId, courtId);
        ValidateCourt(input.Name, input.SlotDurationMinutes);

        court.Name = input.Name.Trim();
        court.Type = Parsing.ParseCourtType(input.Type);
        court.Surface = NormalizeSurface(input.Surface);
        court.Covered = input.Covered;
        court.SlotDurationMinutes = input.SlotDurationMinutes;
        court.Active = input.Active;

        courts.Update(court);
        return Mappers.ToOutput(court, prices.GetByCourt(court.Id));
    }

    /// <summary>Elimina una cancha del dueño y sus tarifas.</summary>
    public void Delete(string ownerId, string courtId)
    {
        var court = Ownership.OwnedCourt(venues, courts, ownerId, courtId);
        prices.DeleteByCourt(court.Id);
        courts.Delete(court.Id);
    }

    /// <summary>Reemplaza el conjunto de tarifas por franja de una cancha.</summary>
    public CourtOutput SetPrices(string ownerId, string courtId, SetPricesInput input)
    {
        var court = Ownership.OwnedCourt(venues, courts, ownerId, courtId);
        prices.DeleteByCourt(courtId);

        foreach (var ruleInput in input.Rules)
        {
            var start = Parsing.ParseTime(ruleInput.StartTime);
            var end = Parsing.ParseTime(ruleInput.EndTime);
            if (end <= start)
                throw new ValidationError("En cada tarifa, la hora de fin debe ser posterior a la de inicio.");
            if (ruleInput.PricePerHour <= 0)
                throw new ValidationError("El precio por hora debe ser mayor a cero.");

            prices.Add(new PriceRule(
                Guid.NewGuid().ToString(),
                courtId,
                Parsing.ParseDayOfWeek(ruleInput.DayOfWeek),
                start,
                end,
                ruleInput.PricePerHour,
                string.IsNullOrWhiteSpace(ruleInput.Kind) ? "normal" : ruleInput.Kind.Trim()));
        }

        return Mappers.ToOutput(court, prices.GetByCourt(courtId));
    }

    private static void ValidateCourt(string name, int slotDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationError("El nombre de la cancha es obligatorio.");
        if (slotDurationMinutes < 30 || slotDurationMinutes % 30 != 0)
            throw new ValidationError("La duración del bloque debe ser múltiplo de 30 minutos.");
    }

    private static string NormalizeSurface(string? surface) =>
        string.IsNullOrWhiteSpace(surface) ? "Sintética" : surface.Trim();
}
