using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de reglas de precio.</summary>
public interface IPriceRuleRepository
{
    /// <summary>Obtiene las reglas de precio de una cancha.</summary>
    IReadOnlyList<PriceRule> GetByCourt(string courtId);

    /// <summary>Agrega una nueva regla de precio.</summary>
    void Add(PriceRule rule);

    /// <summary>Elimina todas las reglas de precio de una cancha.</summary>
    void DeleteByCourt(string courtId);
}
