using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

public interface IReservationRepository
{
    Reservation? GetById(string id);
    IReadOnlyList<Reservation> GetActiveByUser(string userId, DateTime now);
    IReadOnlyList<Reservation> GetActiveByFieldAndDate(string fieldId, DateOnly date);
    int CountActiveByUser(string userId, DateTime now);
    void Add(Reservation reservation);
    void Cancel(string reservationId);
    void AddNoShow(string reservationId, string userId);
}
