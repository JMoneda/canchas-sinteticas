using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de reservas en memoria.</summary>
public class InMemoryReservationRepository(InMemoryDatabase db) : IReservationRepository
{
    /// <inheritdoc/>
    public Reservation? GetById(string id) => db.Reservations.GetValueOrDefault(id);

    /// <inheritdoc/>
    public IReadOnlyList<Reservation> GetActiveByClient(string clientId, DateTime now) =>
        db.Reservations.Values
            .Where(r => r.ClientId == clientId
                && r.Status == ReservationStatus.Confirmed
                && r.StartDateTime >= now)
            .OrderBy(r => r.StartDateTime)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Reservation> GetByClient(string clientId) =>
        db.Reservations.Values
            .Where(r => r.ClientId == clientId)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Reservation> GetActiveByCourtAndDate(string courtId, DateOnly date) =>
        db.Reservations.Values
            .Where(r => r.CourtId == courtId
                && r.Date == date
                && r.Status == ReservationStatus.Confirmed)
            .ToList();

    /// <inheritdoc/>
    public IReadOnlyList<Reservation> GetByCourt(string courtId) =>
        db.Reservations.Values
            .Where(r => r.CourtId == courtId)
            .ToList();

    /// <inheritdoc/>
    public int CountActiveByClient(string clientId, DateTime now) =>
        db.Reservations.Values
            .Count(r => r.ClientId == clientId
                && r.Status == ReservationStatus.Confirmed
                && r.StartDateTime >= now);

    /// <inheritdoc/>
    public void Add(Reservation reservation) => db.Reservations[reservation.Id] = reservation;

    /// <inheritdoc/>
    public void Update(Reservation reservation) => db.Reservations[reservation.Id] = reservation;
}
