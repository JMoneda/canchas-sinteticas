using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Infrastructure.Persistence;
using CanchasSinteticas.Infrastructure.Repositories;

namespace CanchasSinteticas.Tests.Support;

/// <summary>Reloj fijo y avanzable para pruebas deterministas.</summary>
public class FixedClock(DateTime now) : IClock
{
    /// <inheritdoc/>
    public DateTime Now { get; set; } = now;
}

/// <summary>Gateway de pagos falso: registra llamadas y permite simular éxito o caída del proveedor.</summary>
public class FakePaymentGateway : IPaymentGateway
{
    /// <summary>Si es verdadero, CreateTransaction lanza un error de proveedor.</summary>
    public bool ThrowOnCreate { get; set; }

    /// <summary>Última solicitud de reembolso recibida.</summary>
    public bool RefundCalled { get; private set; }

    /// <inheritdoc/>
    public Task<GatewayTransactionResult> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreate)
            throw new global::CanchasSinteticas.Domain.Exceptions.PaymentGatewayError("proveedor caído");

        return Task.FromResult(new GatewayTransactionResult(
            TransactionId: $"TX-{request.Reference}",
            RawStatus: "PENDING",
            Reference: request.Reference,
            CheckoutUrl: $"https://checkout.test/{request.Reference}"));
    }

    /// <inheritdoc/>
    public Task<GatewayTransactionResult> GetTransactionAsync(string transactionId, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GatewayTransactionResult(transactionId, "APPROVED", transactionId, null));

    /// <inheritdoc/>
    public Task<GatewayRefundResult> RefundAsync(string transactionId, decimal amount, PaymentGatewayCredentials credentials, CancellationToken cancellationToken = default)
    {
        RefundCalled = true;
        return Task.FromResult(new GatewayRefundResult($"REFUND-{transactionId}", "APPROVED"));
    }
}

/// <summary>Resolver de credenciales falso.</summary>
public class StubCredentialsResolver : IPaymentGatewayCredentialsResolver
{
    private static readonly PaymentGatewayCredentials Creds =
        new("https://sandbox.test/v1", "pub", "prv", "integrity", "events", null);

    /// <inheritdoc/>
    public PaymentGatewayCredentials Resolve(Venue venue) => Creds;

    /// <inheritdoc/>
    public PaymentGatewayCredentials ResolvePlatform() => Creds;
}

/// <summary>Notificador que registra las notificaciones enviadas.</summary>
public class RecordingNotifier : INotificationSender
{
    /// <summary>Notificaciones recibidas.</summary>
    public List<PaymentNotification> Sent { get; } = [];

    /// <inheritdoc/>
    public Task NotifyAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        Sent.Add(notification);
        return Task.CompletedTask;
    }
}

/// <summary>Verificador de webhook falso: devuelve el evento configurado (o null si no hay).</summary>
public class StubWebhookVerifier : IPaymentWebhookVerifier
{
    /// <summary>Evento a devolver; null simula una firma inválida.</summary>
    public PaymentWebhookEvent? Next { get; set; }

    /// <inheritdoc/>
    public PaymentWebhookEvent? VerifyAndParse(string rawBody) => Next;
}

/// <summary>Generador de comprobantes falso (no produce un PDF real en pruebas).</summary>
public class FakeReceiptGenerator : IReceiptGenerator
{
    /// <inheritdoc/>
    public byte[] GeneratePdf(Receipt receipt) =>
        System.Text.Encoding.UTF8.GetBytes($"PDF:{receipt.Number}");
}

/// <summary>Construye un escenario en memoria (sede, cancha, reserva pendiente y su pago).</summary>
public class TestWorld
{
    public InMemoryDatabase Db { get; } = new();
    public InMemoryPaymentRepository Payments { get; }
    public InMemoryReservationRepository Reservations { get; }
    public InMemoryVenueRepository Venues { get; }
    public InMemoryCourtRepository Courts { get; }
    public InMemoryUserRepository Users { get; }
    public InMemoryMatchRepository Matches { get; }
    public InMemoryPriceRuleRepository Prices { get; }
    public InMemoryBlackoutRepository Blackouts { get; }
    public InMemoryProcessedWebhookEventRepository ProcessedEvents { get; }
    public InMemoryReceiptRepository Receipts { get; }

    public TestWorld()
    {
        Payments = new InMemoryPaymentRepository(Db);
        Reservations = new InMemoryReservationRepository(Db);
        Venues = new InMemoryVenueRepository(Db);
        Courts = new InMemoryCourtRepository(Db);
        Users = new InMemoryUserRepository(Db);
        Matches = new InMemoryMatchRepository(Db);
        Prices = new InMemoryPriceRuleRepository(Db);
        Blackouts = new InMemoryBlackoutRepository(Db);
        ProcessedEvents = new InMemoryProcessedWebhookEventRepository(Db);
        Receipts = new InMemoryReceiptRepository(Db);
    }

    /// <summary>Construye un ReceiptService listo para pruebas.</summary>
    public ReceiptService BuildReceiptService(IClock clock) =>
        new(Receipts, Reservations, Courts, Venues, Users, new FakeReceiptGenerator(), clock);

    /// <summary>Siembra sede, cancha, cliente, reserva pendiente de pago y su pago pendiente.</summary>
    public (Reservation Reservation, Payment Payment) SeedPendingReservation(DateTime now, decimal price = 120000m)
    {
        var owner = new User("owner1", "Dueño", "owner@test.co", null, "x", UserRole.Owner, now);
        var client = new User("client1", "Cliente", "client@test.co", null, "x", UserRole.Client, now);
        Db.Users[owner.Id] = owner;
        Db.Users[client.Id] = client;

        var venue = new Venue("venue1", owner.Id, "Sede", "Medellín", "Calle 1", null, null, [], [],
            new TimeOnly(6, 0), new TimeOnly(23, 0), 2, true, now);
        Db.Venues[venue.Id] = venue;

        var court = new Court("court1", venue.Id, "Cancha 1", CourtType.Futbol5, "sintética", false, 60, true);
        Db.Courts[court.Id] = court;

        var date = DateOnly.FromDateTime(now.AddDays(1));
        var reservation = new Reservation("res1", court.Id, client.Id, null, null, date,
            new TimeOnly(18, 0), new TimeOnly(19, 0), price, ReservationChannel.Online, now, pendingPayment: true);
        Reservations.Add(reservation);

        var payment = new Payment("pay1", reservation.Id, price, PaymentMethod.OnlineGateway,
            PaymentStatus.Pending, null, now);
        Payments.Add(payment);

        return (reservation, payment);
    }

    /// <summary>Siembra un partido con pago dividido sobre una reserva pendiente, con los jugadores dados.</summary>
    public Match SeedSplitMatch(DateTime now, int maxPlayers, decimal totalPrice, params string[] playerIds)
    {
        var owner = new User("owner1", "Dueño", "owner@test.co", null, "x", UserRole.Owner, now);
        Db.Users[owner.Id] = owner;
        var venue = new Venue("venue1", owner.Id, "Sede", "Medellín", "Calle 1", null, null, [], [],
            new TimeOnly(6, 0), new TimeOnly(23, 0), 2, true, now);
        Db.Venues[venue.Id] = venue;
        var court = new Court("court1", venue.Id, "Cancha 1", CourtType.Futbol5, "sintética", false, 60, true);
        Db.Courts[court.Id] = court;

        var date = DateOnly.FromDateTime(now.AddDays(1));
        var start = new TimeOnly(18, 0);
        var reservation = new Reservation("res1", court.Id, playerIds[0], null, null, date,
            start, new TimeOnly(19, 0), totalPrice, ReservationChannel.Online, now, pendingPayment: true);
        Reservations.Add(reservation);

        var match = new Match("match1", reservation.Id, playerIds[0], maxPlayers, true, totalPrice,
            null, reservation.StartDateTime, now);
        foreach (var id in playerIds)
        {
            Db.Users[id] = new User(id, id, $"{id}@test.co", null, "x", UserRole.Client, now);
            match.Join(id, id, now);
        }

        Matches.Add(match);
        return match;
    }
}
