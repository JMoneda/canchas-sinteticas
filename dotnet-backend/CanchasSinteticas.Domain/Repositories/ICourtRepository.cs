using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de canchas.</summary>
public interface ICourtRepository
{
    /// <summary>Obtiene una cancha por su identificador.</summary>
    Court? GetById(string id);

    /// <summary>Obtiene todas las canchas de una sede.</summary>
    IReadOnlyList<Court> GetByVenue(string venueId);

    /// <summary>Agrega una nueva cancha.</summary>
    void Add(Court court);

    /// <summary>Actualiza una cancha existente.</summary>
    void Update(Court court);

    /// <summary>Elimina una cancha.</summary>
    void Delete(string id);
}
