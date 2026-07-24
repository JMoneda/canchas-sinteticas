using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de bloqueos de cancha.</summary>
public interface IBlackoutRepository
{
    /// <summary>Obtiene un bloqueo por su identificador.</summary>
    Blackout? GetById(string id);

    /// <summary>Obtiene los bloqueos de una cancha.</summary>
    IReadOnlyList<Blackout> GetByCourt(string courtId);

    /// <summary>Obtiene los bloqueos de una cancha en una fecha concreta.</summary>
    IReadOnlyList<Blackout> GetByCourtAndDate(string courtId, DateOnly date);

    /// <summary>Agrega un nuevo bloqueo.</summary>
    void Add(Blackout blackout);

    /// <summary>Elimina un bloqueo.</summary>
    void Delete(string id);
}
