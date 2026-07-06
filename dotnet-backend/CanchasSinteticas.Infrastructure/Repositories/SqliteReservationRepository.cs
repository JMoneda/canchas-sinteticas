using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Data;
using CanchasSinteticas.Infrastructure.Data.Models;

namespace CanchasSinteticas.Infrastructure.Repositories;

public class SqliteReservationRepository(AppDbContext db) : IReservationRepository
{
    public Reservation? GetById(string id)
    {
        var m = db.Reservations.Find(id);
        return m is null ? null : ToEntity(m);
    }

    public IReadOnlyList<Reservation> GetActiveByUser(string userId, DateTime now)
    {
        var today = now.Date.ToString("yyyy-MM-dd");
        var currentTime = now.ToString("HH:mm");

        return db.Reservations
            .Where(r => r.UserId == userId && r.Status == "active" &&
                (r.Date.CompareTo(today) > 0 ||
                 (r.Date == today && r.EndTime.CompareTo(currentTime) > 0)))
            .Select(ToEntity)
            .ToList();
    }

    public IReadOnlyList<Reservation> GetActiveByFieldAndDate(string fieldId, DateOnly date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return db.Reservations
            .Where(r => r.FieldId == fieldId && r.Status == "active" && r.Date == dateStr)
            .Select(ToEntity)
            .ToList();
    }

    public int CountActiveByUser(string userId, DateTime now)
    {
        var today = now.Date.ToString("yyyy-MM-dd");
        var currentTime = now.ToString("HH:mm");

        return db.Reservations.Count(r =>
            r.UserId == userId && r.Status == "active" &&
            (r.Date.CompareTo(today) > 0 ||
             (r.Date == today && r.EndTime.CompareTo(currentTime) > 0)));
    }

    public void Add(Reservation reservation)
    {
        db.Reservations.Add(new ReservationModel
        {
            Id = reservation.Id,
            FieldId = reservation.FieldId,
            UserId = reservation.UserId,
            Date = reservation.Date.ToString("yyyy-MM-dd"),
            StartTime = reservation.StartTime.ToString("HH:mm"),
            EndTime = reservation.EndTime.ToString("HH:mm"),
            Status = reservation.Status,
        });
        db.SaveChanges();
    }

    public void Cancel(string reservationId)
    {
        var m = db.Reservations.Find(reservationId);
        if (m is null) return;
        m.Status = "cancelled";
        db.SaveChanges();
    }

    public void AddNoShow(string reservationId, string userId)
    {
        db.NoShows.Add(new NoShowModel
        {
            Id = Guid.NewGuid().ToString(),
            ReservationId = reservationId,
            UserId = userId,
            RecordedAt = DateTime.UtcNow.ToString("o"),
        });
        db.SaveChanges();
    }

    private static Reservation ToEntity(ReservationModel m) => new(
        m.Id, m.FieldId, m.UserId,
        DateOnly.Parse(m.Date),
        TimeOnly.Parse(m.StartTime),
        TimeOnly.Parse(m.EndTime),
        m.Status);
}
