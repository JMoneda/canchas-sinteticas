using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.UseCases;

public class ListAvailableSlotsUseCase(IFieldRepository fieldRepo, IReservationRepository reservationRepo)
{
    private static readonly TimeSpan SlotDuration = TimeSpan.FromHours(1);

    public IReadOnlyList<FieldAvailabilityOutput> Execute(DateOnly queryDate, DateTime now)
    {
        var fields = fieldRepo.GetAll();
        return fields.Select(field =>
        {
            var reservations = reservationRepo.GetActiveByFieldAndDate(field.Id, queryDate);
            var slots = BuildSlots(queryDate, now, reservations);
            return new FieldAvailabilityOutput(field.Id, field.Name, slots);
        }).ToList();
    }

    private static IReadOnlyList<SlotOutput> BuildSlots(
        DateOnly queryDate, DateTime now, IReadOnlyList<Reservation> reservations)
    {
        var slots = new List<SlotOutput>();
        var current = queryDate.ToDateTime(new TimeOnly(6, 0));
        var end = queryDate.ToDateTime(new TimeOnly(23, 0));

        while (current + SlotDuration <= end)
        {
            var slotStart = TimeOnly.FromDateTime(current);
            var slotEnd = TimeOnly.FromDateTime(current + SlotDuration);

            var occupied = reservations.Any(r => r.StartTime < slotEnd && slotStart < r.EndTime);
            var bookable = current - now >= TimeSpan.FromHours(1);

            if (!occupied && bookable)
                slots.Add(new SlotOutput(slotStart.ToString("HH:mm"), slotEnd.ToString("HH:mm")));

            current += SlotDuration;
        }

        return slots;
    }
}
