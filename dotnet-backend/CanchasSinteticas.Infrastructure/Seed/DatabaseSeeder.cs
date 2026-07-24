using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Services;
using CanchasSinteticas.Domain.ValueObjects;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Seed;

/// <summary>
/// Carga datos de demostración en el almacén en memoria: dueños, un cliente,
/// sedes en Bogotá y Medellín, canchas con tarifas diurno/nocturno y un par de reservas.
/// Contraseña de todas las cuentas de ejemplo: <c>password123</c>.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>Puebla la base en memoria si aún está vacía.</summary>
    public static void Seed(InMemoryDatabase db, IPasswordHasher hasher, IClock clock)
    {
        if (!db.Users.IsEmpty)
            return;

        var now = clock.Now;
        var password = hasher.Hash("password123");

        // --- Usuarios ---
        db.Users["owner-1"] = new User("owner-1", "Juan Pérez", "owner1@canchas.co", "3001112233", password, UserRole.Owner, now);
        db.Users["owner-2"] = new User("owner-2", "María Gómez", "owner2@canchas.co", "3004445566", password, UserRole.Owner, now);
        db.Users["client-1"] = new User("client-1", "Carlos Ruiz", "cliente@canchas.co", "3007778899", password, UserRole.Client, now);
        db.Users["admin-1"] = new User("admin-1", "Administrador", "admin@canchas.co", null, password, UserRole.SuperAdmin, now);

        // --- Sedes del dueño 1 (Bogotá) ---
        AddVenue(db, "venue-1", "owner-1", "Complejo Deportivo La 80", "Bogotá",
            "Calle 80 #45-30", new GeoLocation(4.6871, -74.0930), "6015551010",
            ["parqueo", "iluminación", "camerinos", "cafetería"], now);

        AddCourt(db, "court-1", "venue-1", "Cancha 1 - Techada", CourtType.Futbol5, "Sintética", true, 60, now,
            diurno: 60000, nocturno: 90000);
        AddCourt(db, "court-2", "venue-1", "Cancha 2", CourtType.Futbol7, "Sintética", false, 60, now,
            diurno: 90000, nocturno: 130000);

        AddVenue(db, "venue-2", "owner-1", "Arena Norte", "Bogotá",
            "Autopista Norte #120-15", new GeoLocation(4.7300, -74.0450), "6015552020",
            ["parqueo", "iluminación", "graderías"], now);

        AddCourt(db, "court-3", "venue-2", "Cancha Grande", CourtType.Futbol11, "Grama sintética", false, 90, now,
            diurno: 180000, nocturno: 250000);

        // --- Sede del dueño 2 (Medellín) ---
        AddVenue(db, "venue-3", "owner-2", "Fútbol Park Medellín", "Medellín",
            "Carrera 43A #18-20", new GeoLocation(6.2100, -75.5700), "6045553030",
            ["parqueo", "iluminación", "camerinos", "tienda"], now);

        AddCourt(db, "court-4", "venue-3", "Cancha A", CourtType.Futbol5, "Sintética", true, 60, now,
            diurno: 55000, nocturno: 85000);
        AddCourt(db, "court-5", "venue-3", "Cancha B", CourtType.Futbol8, "Sintética", false, 60, now,
            diurno: 110000, nocturno: 150000);

        // --- Reservas de ejemplo (para mañana) ---
        var tomorrow = DateOnly.FromDateTime(now).AddDays(1);
        AddReservation(db, "res-1", "court-1", "client-1", tomorrow, new TimeOnly(19, 0), new TimeOnly(20, 0), now);
        AddReservation(db, "res-2", "court-4", "client-1", tomorrow, new TimeOnly(20, 0), new TimeOnly(21, 0), now);

        // Partido abierto de ejemplo (con pago dividido) sobre la reserva res-1.
        var res1 = db.Reservations["res-1"];
        var match = new Match(
            "match-1", "res-1", "client-1", 10, true, res1.TotalPrice,
            "Nivel intermedio · faltan jugadores", res1.StartDateTime, now);
        match.Join("client-1", "Carlos Ruiz", now);
        db.Matches["match-1"] = match;
    }

    private static void AddVenue(
        InMemoryDatabase db,
        string id,
        string ownerId,
        string name,
        string city,
        string address,
        GeoLocation location,
        string phone,
        List<string> services,
        DateTime now)
    {
        db.Venues[id] = new Venue(
            id, ownerId, name, city, address, location, phone,
            [],
            services,
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            3,
            true,
            now);
    }

    private static void AddCourt(
        InMemoryDatabase db,
        string id,
        string venueId,
        string name,
        CourtType type,
        string surface,
        bool covered,
        int slotMinutes,
        DateTime now,
        decimal diurno,
        decimal nocturno)
    {
        db.Courts[id] = new Court(id, venueId, name, type, surface, covered, slotMinutes, true);

        db.PriceRules[$"{id}-price-day"] = new PriceRule(
            $"{id}-price-day", id, null, new TimeOnly(6, 0), new TimeOnly(18, 0), diurno, "diurno");
        db.PriceRules[$"{id}-price-night"] = new PriceRule(
            $"{id}-price-night", id, null, new TimeOnly(18, 0), new TimeOnly(23, 0), nocturno, "nocturno");
    }

    private static void AddReservation(
        InMemoryDatabase db,
        string id,
        string courtId,
        string clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        DateTime now)
    {
        var rules = db.PriceRules.Values.Where(r => r.CourtId == courtId).ToList();
        var price = PricingCalculator.Calculate(date, start, end, rules);

        db.Reservations[id] = new Reservation(
            id, courtId, clientId, null, null, date, start, end, price, ReservationChannel.Online, now);

        var payment = new Payment(
            $"{id}-payment", id, price, PaymentMethod.OnlineGateway, PaymentStatus.Pending, null, now);
        db.Payments[payment.Id] = payment;
    }
}
