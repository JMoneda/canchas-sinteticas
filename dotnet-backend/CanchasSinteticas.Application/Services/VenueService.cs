using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Domain.Services;
using CanchasSinteticas.Domain.ValueObjects;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de sedes: búsqueda pública (marketplace) y gestión del dueño.
/// </summary>
public class VenueService(
    IVenueRepository venues,
    ICourtRepository courts,
    IPriceRuleRepository prices,
    IClock clock)
{
    /// <summary>Busca sedes activas para el marketplace, opcionalmente por ciudad.</summary>
    public IReadOnlyList<VenueSummaryOutput> Search(string? city) =>
        venues.Search(city).Select(BuildSummary).ToList();

    /// <summary>Obtiene el detalle público de una sede con sus canchas.</summary>
    public VenueDetailOutput GetDetail(string venueId)
    {
        var venue = venues.GetById(venueId) ?? throw new VenueNotFoundError();
        return BuildDetail(venue);
    }

    /// <summary>Lista las sedes de un dueño.</summary>
    public IReadOnlyList<VenueDetailOutput> GetByOwner(string ownerId) =>
        venues.GetByOwner(ownerId).Select(BuildDetail).ToList();

    /// <summary>Crea una sede para el dueño indicado.</summary>
    public VenueDetailOutput Create(string ownerId, CreateVenueInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new ValidationError("El nombre de la sede es obligatorio.");
        if (string.IsNullOrWhiteSpace(input.City))
            throw new ValidationError("La ciudad es obligatoria.");
        if (string.IsNullOrWhiteSpace(input.Address))
            throw new ValidationError("La dirección es obligatoria.");
        ContactValidation.EnsurePhoneValid(input.Phone);

        var opening = Parsing.ParseTime(input.OpeningTime);
        var closing = Parsing.ParseTime(input.ClosingTime);
        if (closing <= opening)
            throw new ValidationError("La hora de cierre debe ser posterior a la de apertura.");

        var venue = new Venue(
            Guid.NewGuid().ToString(),
            ownerId,
            input.Name.Trim(),
            input.City.Trim(),
            input.Address.Trim(),
            BuildLocation(input.Latitude, input.Longitude),
            ContactValidation.Normalize(input.Phone),
            input.Photos?.ToList() ?? [],
            input.Services?.ToList() ?? [],
            opening,
            closing,
            input.CancellationWindowHours <= 0 ? 3 : input.CancellationWindowHours,
            true,
            clock.Now);

        venues.Add(venue);
        return BuildDetail(venue);
    }

    /// <summary>Actualiza una sede del dueño.</summary>
    public VenueDetailOutput Update(string ownerId, string venueId, UpdateVenueInput input)
    {
        var venue = Ownership.OwnedVenue(venues, ownerId, venueId);
        ContactValidation.EnsurePhoneValid(input.Phone);

        var opening = Parsing.ParseTime(input.OpeningTime);
        var closing = Parsing.ParseTime(input.ClosingTime);
        if (closing <= opening)
            throw new ValidationError("La hora de cierre debe ser posterior a la de apertura.");

        venue.Name = input.Name.Trim();
        venue.City = input.City.Trim();
        venue.Address = input.Address.Trim();
        venue.Location = BuildLocation(input.Latitude, input.Longitude);
        venue.Phone = ContactValidation.Normalize(input.Phone);
        venue.Photos = input.Photos?.ToList() ?? [];
        venue.Services = input.Services?.ToList() ?? [];
        venue.OpeningTime = opening;
        venue.ClosingTime = closing;
        venue.CancellationWindowHours = input.CancellationWindowHours <= 0 ? 3 : input.CancellationWindowHours;
        venue.Active = input.Active;

        venues.Update(venue);
        return BuildDetail(venue);
    }

    /// <summary>Elimina una sede del dueño y sus canchas asociadas.</summary>
    public void Delete(string ownerId, string venueId)
    {
        Ownership.OwnedVenue(venues, ownerId, venueId);
        foreach (var court in courts.GetByVenue(venueId))
        {
            prices.DeleteByCourt(court.Id);
            courts.Delete(court.Id);
        }

        venues.Delete(venueId);
    }

    private static GeoLocation? BuildLocation(double? latitude, double? longitude) =>
        latitude.HasValue && longitude.HasValue
            ? new GeoLocation(latitude.Value, longitude.Value)
            : null;

    private decimal? MinPrice(IReadOnlyList<Court> venueCourts)
    {
        var courtMins = venueCourts
            .Select(c => PricingCalculator.MinPricePerHour(prices.GetByCourt(c.Id)))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        return courtMins.Count > 0 ? courtMins.Min() : null;
    }

    private VenueSummaryOutput BuildSummary(Venue venue)
    {
        var venueCourts = courts.GetByVenue(venue.Id);
        return new VenueSummaryOutput(
            venue.Id,
            venue.Name,
            venue.City,
            venue.Address,
            venue.Location?.Latitude,
            venue.Location?.Longitude,
            venue.Phone,
            venue.Photos,
            venue.Services,
            MinPrice(venueCourts),
            venueCourts.Count);
    }

    private VenueDetailOutput BuildDetail(Venue venue)
    {
        var courtSummaries = courts.GetByVenue(venue.Id)
            .Select(c => Mappers.ToSummary(c, PricingCalculator.MinPricePerHour(prices.GetByCourt(c.Id))))
            .ToList();

        return new VenueDetailOutput(
            venue.Id,
            venue.OwnerId,
            venue.Name,
            venue.City,
            venue.Address,
            venue.Location?.Latitude,
            venue.Location?.Longitude,
            venue.Phone,
            venue.Photos,
            venue.Services,
            Mappers.Time(venue.OpeningTime),
            Mappers.Time(venue.ClosingTime),
            venue.CancellationWindowHours,
            venue.Active,
            courtSummaries);
    }
}
