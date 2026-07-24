using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de sedes en memoria.</summary>
public class InMemoryVenueRepository(InMemoryDatabase db) : IVenueRepository
{
    /// <inheritdoc/>
    public Venue? GetById(string id) => db.Venues.GetValueOrDefault(id);

    /// <inheritdoc/>
    public IReadOnlyList<Venue> GetByOwner(string ownerId) =>
        db.Venues.Values
            .Where(v => v.OwnerId == ownerId)
            .OrderBy(v => v.Name)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Venue> Search(string? city) =>
        db.Venues.Values
            .Where(v => v.Active)
            .Where(v => string.IsNullOrWhiteSpace(city)
                || v.City.Contains(city.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.Name)
            .ToList();

    /// <inheritdoc/>
    public void Add(Venue venue) => db.Venues[venue.Id] = venue;

    /// <inheritdoc/>
    public void Update(Venue venue) => db.Venues[venue.Id] = venue;

    /// <inheritdoc/>
    public void Delete(string id) => db.Venues.TryRemove(id, out _);
}
