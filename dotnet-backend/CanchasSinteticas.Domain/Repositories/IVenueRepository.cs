using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de sedes.</summary>
public interface IVenueRepository
{
    /// <summary>Obtiene una sede por su identificador.</summary>
    Venue? GetById(string id);

    /// <summary>Obtiene todas las sedes de un dueño.</summary>
    IReadOnlyList<Venue> GetByOwner(string ownerId);

    /// <summary>
    /// Busca sedes activas para el marketplace, opcionalmente filtradas por ciudad.
    /// </summary>
    IReadOnlyList<Venue> Search(string? city);

    /// <summary>Agrega una nueva sede.</summary>
    void Add(Venue venue);

    /// <summary>Actualiza una sede existente.</summary>
    void Update(Venue venue);

    /// <summary>Elimina una sede.</summary>
    void Delete(string id);
}
