using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de bloqueos en memoria.</summary>
public class InMemoryBlackoutRepository(InMemoryDatabase db) : IBlackoutRepository
{
    /// <inheritdoc/>
    public Blackout? GetById(string id) => db.Blackouts.GetValueOrDefault(id);

    /// <inheritdoc/>
    public IReadOnlyList<Blackout> GetByCourt(string courtId) =>
        db.Blackouts.Values
            .Where(b => b.CourtId == courtId)
            .OrderBy(b => b.Date)
            .ThenBy(b => b.StartTime)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Blackout> GetByCourtAndDate(string courtId, DateOnly date) =>
        db.Blackouts.Values
            .Where(b => b.CourtId == courtId && b.Date == date)
            .ToList();

    /// <inheritdoc/>
    public void Add(Blackout blackout) => db.Blackouts[blackout.Id] = blackout;

    /// <inheritdoc/>
    public void Delete(string id) => db.Blackouts.TryRemove(id, out _);
}
