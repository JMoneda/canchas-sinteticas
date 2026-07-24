using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de partidos abiertos.</summary>
public interface IMatchRepository
{
    /// <summary>Obtiene un partido por su identificador.</summary>
    Match? GetById(string id);

    /// <summary>Obtiene el partido asociado a una reserva, si existe.</summary>
    Match? GetByReservation(string reservationId);

    /// <summary>Obtiene los partidos activos (abiertos o completos).</summary>
    IReadOnlyList<Match> GetActive();

    /// <summary>Agrega un nuevo partido.</summary>
    void Add(Match match);

    /// <summary>Actualiza un partido existente.</summary>
    void Update(Match match);
}
