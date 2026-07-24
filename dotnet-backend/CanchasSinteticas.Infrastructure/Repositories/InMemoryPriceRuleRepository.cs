using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de reglas de precio en memoria.</summary>
public class InMemoryPriceRuleRepository(InMemoryDatabase db) : IPriceRuleRepository
{
    /// <inheritdoc/>
    public IReadOnlyList<PriceRule> GetByCourt(string courtId) =>
        db.PriceRules.Values
            .Where(r => r.CourtId == courtId)
            .OrderBy(r => r.StartTime)
            .ToList();

    /// <inheritdoc/>
    public void Add(PriceRule rule) => db.PriceRules[rule.Id] = rule;

    /// <inheritdoc/>
    public void DeleteByCourt(string courtId)
    {
        foreach (var rule in db.PriceRules.Values.Where(r => r.CourtId == courtId).ToList())
            db.PriceRules.TryRemove(rule.Id, out _);
    }
}
