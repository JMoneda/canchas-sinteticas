using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Policies;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Domain.Services;
using CanchasSinteticas.Domain.ValueObjects;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de reservas: creación desde el marketplace, listado y cancelación
/// para el cliente, y agenda / creación manual para el dueño.
/// </summary>
public class ReservationService(
    IReservationRepository reservations,
    IVenueRepository venues,
    ICourtRepository courts,
    IPriceRuleRepository prices,
    IBlackoutRepository blackouts,
    IPaymentRepository payments,
    IClock clock)
{
    private const string WalkInClientId = "walk-in";

    /// <summary>Crea una reserva online para un cliente.</summary>
    public ReservationOutput Create(string clientId, CreateReservationInput input)
    {
        var court = courts.GetById(input.CourtId) ?? throw new CourtNotFoundError();
        var venue = venues.GetById(court.VenueId) ?? throw new VenueNotFoundError();

        var slot = BuildSlot(input.Date, input.StartTime, input.EndTime);
        ValidateSlotShape(slot, court, venue);

        if (!slot.IsBookable(clock.Now, ReservationPolicy.MinAdvanceMinutes))
            throw new AdvanceNoticeError();

        if (reservations.CountActiveByClient(clientId, clock.Now) >= ReservationPolicy.MaxActivePerClient)
            throw new ActiveLimitError();

        return Place(court, venue, slot, clientId, null, null, ReservationChannel.Online, Parsing.ParsePaymentMethod(input.PaymentMethod));
    }

    /// <summary>Crea una reserva manual (walk-in / teléfono) desde el panel del dueño.</summary>
    public ReservationOutput CreateManual(string ownerId, ManualReservationInput input)
    {
        var court = Ownership.OwnedCourt(venues, courts, ownerId, input.CourtId);
        var venue = venues.GetById(court.VenueId) ?? throw new VenueNotFoundError();

        var slot = BuildSlot(input.Date, input.StartTime, input.EndTime);
        ValidateSlotShape(slot, court, venue);

        var clientName = string.IsNullOrWhiteSpace(input.ClientName) ? null : input.ClientName.Trim();
        var clientPhone = string.IsNullOrWhiteSpace(input.ClientPhone) ? null : input.ClientPhone.Trim();

        return Place(court, venue, slot, WalkInClientId, clientName, clientPhone, ReservationChannel.Manual, PaymentMethod.Cash);
    }

    /// <summary>Lista el historial de reservas de un cliente (más recientes primero).</summary>
    public IReadOnlyList<ReservationOutput> ListByClient(string clientId) =>
        reservations.GetByClient(clientId)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.StartTime)
            .Select(BuildOutput)
            .ToList();

    /// <summary>Lista las reservas de todas las canchas de un dueño, opcionalmente por fecha.</summary>
    public IReadOnlyList<ReservationOutput> ListByOwner(string ownerId, DateOnly? date)
    {
        var result = new List<ReservationOutput>();

        foreach (var venue in venues.GetByOwner(ownerId))
        {
            foreach (var court in courts.GetByVenue(venue.Id))
            {
                foreach (var reservation in reservations.GetByCourt(court.Id))
                {
                    if (date is null || reservation.Date == date)
                        result.Add(BuildOutput(reservation, court, venue, payments.GetByReservation(reservation.Id)));
                }
            }
        }

        return result
            .OrderBy(r => r.Date)
            .ThenBy(r => r.StartTime)
            .ToList();
    }

    /// <summary>Cancela una reserva del cliente aplicando la política de la sede.</summary>
    public CancelOutput Cancel(string clientId, string reservationId)
    {
        var reservation = reservations.GetById(reservationId) ?? throw new NotFoundError();
        if (reservation.ClientId != clientId)
            throw new NotAuthorizedError();

        var court = courts.GetById(reservation.CourtId);
        var venue = court is null ? null : venues.GetById(court.VenueId);
        var windowHours = venue?.CancellationWindowHours ?? ReservationPolicy.DefaultCancellationWindowHours;

        var isLate = reservation.StartDateTime - clock.Now < TimeSpan.FromHours(windowHours);
        reservation.Cancel(isLate);
        reservations.Update(reservation);

        var refunded = false;
        var payment = payments.GetByReservation(reservationId);
        if (payment is not null && payment.Status == PaymentStatus.Paid && !isLate)
        {
            payment.Refund();
            payments.Update(payment);
            refunded = true;
        }

        return new CancelOutput(reservation.Id, reservation.Status.ToString(), isLate, refunded);
    }

    private TimeSlot BuildSlot(string date, string startTime, string endTime) =>
        new(Parsing.ParseDate(date), Parsing.ParseTime(startTime), Parsing.ParseTime(endTime));

    private ReservationOutput Place(
        Court court,
        Venue venue,
        TimeSlot slot,
        string clientId,
        string? clientName,
        string? clientPhone,
        ReservationChannel channel,
        PaymentMethod method)
    {
        EnsureFree(court.Id, slot);

        var price = PricingCalculator.Calculate(slot.Date, slot.StartTime, slot.EndTime, prices.GetByCourt(court.Id));
        var reservation = new Reservation(
            Guid.NewGuid().ToString(),
            court.Id,
            clientId,
            clientName,
            clientPhone,
            slot.Date,
            slot.StartTime,
            slot.EndTime,
            price,
            channel,
            clock.Now);
        reservations.Add(reservation);

        var payment = new Payment(
            Guid.NewGuid().ToString(),
            reservation.Id,
            price,
            method,
            PaymentStatus.Pending,
            null,
            clock.Now);
        payments.Add(payment);

        return BuildOutput(reservation, court, venue, payment);
    }

    private void ValidateSlotShape(TimeSlot slot, Court court, Venue venue)
    {
        var minutes = slot.Duration.TotalMinutes;
        if (minutes < court.SlotDurationMinutes || minutes % court.SlotDurationMinutes != 0)
            throw new DurationError();

        if (!slot.WithinOperatingHours(venue.OpeningTime, venue.ClosingTime))
            throw new OperatingHoursError();
    }

    private void EnsureFree(string courtId, TimeSlot slot)
    {
        foreach (var existing in reservations.GetActiveByCourtAndDate(courtId, slot.Date))
        {
            var existingSlot = new TimeSlot(existing.Date, existing.StartTime, existing.EndTime);
            if (slot.OverlapsWith(existingSlot))
                throw new OverlapError();
        }

        foreach (var blackout in blackouts.GetByCourtAndDate(courtId, slot.Date))
        {
            if (blackout.Covers(slot.Date, slot.StartTime, slot.EndTime))
                throw new BlackoutConflictError();
        }
    }

    private ReservationOutput BuildOutput(Reservation reservation)
    {
        var court = courts.GetById(reservation.CourtId);
        var venue = court is null ? null : venues.GetById(court.VenueId);
        return BuildOutput(reservation, court, venue, payments.GetByReservation(reservation.Id));
    }

    private ReservationOutput BuildOutput(Reservation reservation, Court? court, Venue? venue, Payment? payment) =>
        new(
            reservation.Id,
            reservation.CourtId,
            court?.Name ?? string.Empty,
            court?.VenueId ?? string.Empty,
            venue?.Name ?? string.Empty,
            reservation.ClientId,
            reservation.ClientName,
            reservation.ClientPhone,
            Mappers.Date(reservation.Date),
            Mappers.Time(reservation.StartTime),
            Mappers.Time(reservation.EndTime),
            reservation.TotalPrice,
            reservation.Status.ToString(),
            reservation.Channel.ToString(),
            (payment?.Status ?? PaymentStatus.Pending).ToString(),
            reservation.CreatedAt.ToString("s"));
}
