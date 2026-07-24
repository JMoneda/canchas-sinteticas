using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de partidos abiertos en memoria.</summary>
public class InMemoryMatchRepository(InMemoryDatabase db) : IMatchRepository
{
    /// <inheritdoc/>
    public Match? GetById(string id) => db.Matches.GetValueOrDefault(id);

    /// <inheritdoc/>
    public Match? GetByReservation(string reservationId) =>
        db.Matches.Values.FirstOrDefault(m => m.ReservationId == reservationId);

    /// <inheritdoc/>
    public IReadOnlyList<Match> GetActive() =>
        db.Matches.Values
            .Where(m => m.Status is MatchStatus.Open or MatchStatus.Full)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

    /// <inheritdoc/>
    public void Add(Match match) => db.Matches[match.Id] = match;

    /// <inheritdoc/>
    public void Update(Match match) => db.Matches[match.Id] = match;
}
