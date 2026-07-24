using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Policies;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Domain.Services;
using CanchasSinteticas.Domain.ValueObjects;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Genera la disponibilidad de una cancha para una fecha, combinando horario de
/// operación de la sede, duración de bloque, reservas existentes y bloqueos.
/// </summary>
public class AvailabilityService(
    IVenueRepository venues,
    ICourtRepository courts,
    IPriceRuleRepository prices,
    IBlackoutRepository blackouts,
    IReservationRepository reservations,
    IClock clock)
{
    /// <summary>Devuelve los slots de una cancha en la fecha indicada.</summary>
    public CourtAvailabilityOutput GetCourtAvailability(string courtId, DateOnly date)
    {
        var court = courts.GetById(courtId) ?? throw new CourtNotFoundError();
        var venue = venues.GetById(court.VenueId) ?? throw new VenueNotFoundError();

        var courtPrices = prices.GetByCourt(courtId);
        var dayReservations = reservations.GetActiveByCourtAndDate(courtId, date);
        var dayBlackouts = blackouts.GetByCourtAndDate(courtId, date);
        var now = clock.Now;

        var openMinutes = (venue.OpeningTime.Hour * 60) + venue.OpeningTime.Minute;
        var closeMinutes = (venue.ClosingTime.Hour * 60) + venue.ClosingTime.Minute;
        var duration = court.SlotDurationMinutes;

        var slots = new List<SlotOutput>();

        for (var startMinutes = openMinutes; startMinutes + duration <= closeMinutes; startMinutes += duration)
        {
            var endMinutes = startMinutes + duration;
            var start = new TimeOnly(startMinutes / 60, startMinutes % 60);
            var end = new TimeOnly(endMinutes / 60, endMinutes % 60);
            var slot = new TimeSlot(date, start, end);

            decimal price;
            try
            {
                price = PricingCalculator.Calculate(date, start, end, courtPrices);
            }
            catch (NoPriceConfiguredError)
            {
                price = 0m;
            }

            string status;
            bool available;

            if (dayReservations.Any(r => slot.OverlapsWith(new TimeSlot(date, r.StartTime, r.EndTime))))
            {
                status = "reserved";
                available = false;
            }
            else if (dayBlackouts.Any(b => b.Covers(date, start, end)))
            {
                status = "blocked";
                available = false;
            }
            else if (!slot.IsBookable(now, ReservationPolicy.MinAdvanceMinutes))
            {
                status = "past";
                available = false;
            }
            else
            {
                status = "available";
                available = true;
            }

            slots.Add(new SlotOutput(Mappers.Time(start), Mappers.Time(end), price, available, status));
        }

        return new CourtAvailabilityOutput(
            court.Id,
            court.Name,
            court.Type.ToString(),
            Mappers.Date(date),
            slots);
    }
}
