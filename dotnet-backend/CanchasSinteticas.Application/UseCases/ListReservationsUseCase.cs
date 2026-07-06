using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.UseCases;

public class ListReservationsUseCase(IFieldRepository fieldRepo, IReservationRepository reservationRepo)
{
    public IReadOnlyList<ReservationOutput> Execute(string userId, DateTime now)
    {
        var reservations = reservationRepo.GetActiveByUser(userId, now);
        var fields = fieldRepo.GetAll().ToDictionary(f => f.Id, f => f.Name);

        return reservations.Select(r => new ReservationOutput(
            r.Id,
            r.FieldId,
            fields.GetValueOrDefault(r.FieldId, r.FieldId),
            r.UserId,
            r.Date.ToString("yyyy-MM-dd"),
            r.StartTime.ToString("HH:mm"),
            r.EndTime.ToString("HH:mm"),
            r.Status)).ToList();
    }
}
