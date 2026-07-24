using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de canchas en memoria.</summary>
public class InMemoryCourtRepository(InMemoryDatabase db) : ICourtRepository
{
    /// <inheritdoc/>
    public Court? GetById(string id) => db.Courts.GetValueOrDefault(id);

    /// <inheritdoc/>
    public IReadOnlyList<Court> GetByVenue(string venueId) =>
        db.Courts.Values
            .Where(c => c.VenueId == venueId)
            .OrderBy(c => c.Name)
            .ToList();

    /// <inheritdoc/>
    public void Add(Court court) => db.Courts[court.Id] = court;

    /// <inheritdoc/>
    public void Update(Court court) => db.Courts[court.Id] = court;

    /// <inheritdoc/>
    public void Delete(string id) => db.Courts.TryRemove(id, out _);
}
