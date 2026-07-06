using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Domain.ValueObjects;

namespace CanchasSinteticas.Application.UseCases;

public class CreateReservationUseCase(IFieldRepository fieldRepo, IReservationRepository reservationRepo)
{
    public ReservationOutput Execute(CreateReservationInput input, DateTime now)
    {
        var fields = fieldRepo.GetAll();
        var field = fields.FirstOrDefault(f => f.Id == input.FieldId)
            ?? throw new FieldNotFoundError();

        var date = DateOnly.Parse(input.Date);
        var start = TimeOnly.Parse(input.StartTime);
        var end = TimeOnly.Parse(input.EndTime);

        var slot = new TimeSlot(date, start, end);

        if (!slot.IsBookable(now))
            throw new AdvanceNoticeError();

        if (reservationRepo.CountActiveByUser(input.UserId, now) >= 2)
            throw new ActiveLimitError();

        var existing = reservationRepo.GetActiveByFieldAndDate(input.FieldId, date);
        foreach (var r in existing)
        {
            var existingSlot = new TimeSlot(r.Date, r.StartTime, r.EndTime);
            if (slot.OverlapsWith(existingSlot))
                throw new OverlapError();
        }

        var reservationId = Guid.NewGuid().ToString();
        var reservation = new Reservation(reservationId, input.FieldId, input.UserId, date, start, end, "active");
        reservationRepo.Add(reservation);

        return new ReservationOutput(
            reservationId, field.Id, field.Name, input.UserId,
            date.ToString("yyyy-MM-dd"), start.ToString("HH:mm"), end.ToString("HH:mm"), "active");
    }
}
