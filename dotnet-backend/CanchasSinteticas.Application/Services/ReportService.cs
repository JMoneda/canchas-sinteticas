using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Genera reportes agregados de ocupación e ingresos para el dueño.
/// </summary>
public class ReportService(
    IVenueRepository venues,
    ICourtRepository courts,
    IReservationRepository reservations,
    IPaymentRepository payments,
    IClock clock)
{

    /// <summary>Calcula el reporte del dueño en el rango indicado (por defecto, últimos 30 días).</summary>
    public OwnerReportOutput GetOwnerReport(string ownerId, DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(clock.Now);
        var fromDate = from ?? today.AddDays(-30);
        var toDate = to ?? today.AddDays(30);
        var days = Math.Max(1, toDate.DayNumber - fromDate.DayNumber + 1);

        var byCourt = new List<CourtReportOutput>();
        var hourCounts = new Dictionary<int, int>();
        var totalReservations = 0;
        var totalRevenue = 0m;
        var bookedHours = 0d;
        var capacityHours = 0d;

        foreach (var venue in venues.GetByOwner(ownerId))
        {
            var venueOpenHours = (venue.ClosingTime - venue.OpeningTime).TotalHours;

            foreach (var court in courts.GetByVenue(venue.Id))
            {
                capacityHours += venueOpenHours * days;

                var courtReservations = 0;
                var courtRevenue = 0m;

                foreach (var reservation in reservations.GetByCourt(court.Id))
                {
                    if (reservation.Date < fromDate || reservation.Date > toDate)
                        continue;

                    // Solo cuenta como ingreso el dinero realmente cobrado (pago aprobado y no reembolsado),
                    // atribuido a la sede/dueño (FR-028). Pendientes, expirados y reembolsados no suman.
                    var payment = payments.GetByReservation(reservation.Id);
                    if (payment is null || payment.Status != PaymentStatus.Paid)
                        continue;

                    courtReservations++;
                    courtRevenue += reservation.TotalPrice;
                    bookedHours += (reservation.EndTime - reservation.StartTime).TotalHours;

                    var hour = reservation.StartTime.Hour;
                    hourCounts[hour] = hourCounts.GetValueOrDefault(hour) + 1;
                }

                totalReservations += courtReservations;
                totalRevenue += courtRevenue;
                byCourt.Add(new CourtReportOutput(court.Id, court.Name, venue.Name, courtReservations, courtRevenue));
            }
        }

        var occupancy = capacityHours > 0 ? Math.Round(bookedHours / capacityHours, 4) : 0d;

        var topHours = hourCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(5)
            .Select(kv => new HourStatOutput($"{kv.Key:D2}:00", kv.Value))
            .ToList();

        return new OwnerReportOutput(
            Mappers.Date(fromDate),
            Mappers.Date(toDate),
            totalReservations,
            totalRevenue,
            occupancy,
            byCourt.OrderByDescending(c => c.Revenue).ToList(),
            topHours);
    }
}
