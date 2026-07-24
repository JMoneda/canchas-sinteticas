using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas del pago dividido a nivel de servicio + webhook (US2, T038).</summary>
public class MatchPayShareTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static MatchService BuildMatchService(TestWorld w)
    {
        var clock = new FixedClock(Now);
        var reservationService = new ReservationService(
            w.Reservations, w.Venues, w.Courts, w.Prices, w.Blackouts, w.Payments,
            new FakePaymentGateway(), new StubCredentialsResolver(), clock);
        return new MatchService(
            w.Matches, w.Reservations, w.Courts, w.Venues, w.Users, w.Payments,
            new FakePaymentGateway(), new StubCredentialsResolver(), new PaymentSettings(15),
            reservationService, clock);
    }

    private static PaymentWebhookService BuildWebhook(TestWorld w, StubWebhookVerifier verifier)
    {
        var clock = new FixedClock(Now);
        return new(verifier, w.Payments, w.Reservations, w.Courts, w.Matches, w.ProcessedEvents,
            new FakePaymentGateway(), new StubCredentialsResolver(), w.Venues, new RecordingNotifier(),
            w.BuildReceiptService(clock), clock);
    }

    [Fact]
    public async Task PayShare_crea_pago_de_parte_en_processing()
    {
        var w = new TestWorld();
        w.SeedSplitMatch(Now, maxPlayers: 2, totalPrice: 80000m, "a", "b");
        var svc = BuildMatchService(w);

        var result = await svc.PayShareAsync("b", "match1", new PayInput("nequi"));

        Assert.Equal("Processing", result.Status);
        Assert.Equal(40000m, result.Amount);
        var payment = w.Payments.GetByMatchAndPayer("match1", "b")!;
        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal("match1", payment.MatchId);
    }

    [Fact]
    public async Task PayShare_repetida_tras_pagar_es_rechazada()
    {
        var w = new TestWorld();
        w.SeedSplitMatch(Now, 2, 80000m, "a", "b");
        var svc = BuildMatchService(w);

        var init = await svc.PayShareAsync("b", "match1", new PayInput("nequi"));

        // Confirmar por webhook.
        var verifier = new StubWebhookVerifier
        {
            Next = new PaymentWebhookEvent("evt-b", $"TX-{init.PaymentId}", init.PaymentId, PaymentWebhookStatus.Approved, "APPROVED"),
        };
        await BuildWebhook(w, verifier).ProcessAsync("{}");

        await Assert.ThrowsAsync<InvalidPaymentTransitionError>(
            () => svc.PayShareAsync("b", "match1", new PayInput("nequi")));
    }

    [Fact]
    public async Task Cuando_todas_las_partes_pagan_se_confirma_la_reserva_del_partido()
    {
        var w = new TestWorld();
        w.SeedSplitMatch(Now, 2, 80000m, "a", "b");
        var svc = BuildMatchService(w);

        foreach (var user in new[] { "a", "b" })
        {
            var init = await svc.PayShareAsync(user, "match1", new PayInput("nequi"));
            var verifier = new StubWebhookVerifier
            {
                Next = new PaymentWebhookEvent($"evt-{user}", $"TX-{init.PaymentId}", init.PaymentId, PaymentWebhookStatus.Approved, "APPROVED"),
            };
            await BuildWebhook(w, verifier).ProcessAsync("{}");
        }

        Assert.True(w.Matches.GetById("match1")!.IsFullyCollected);
        Assert.Equal(ReservationStatus.Confirmed, w.Reservations.GetById("res1")!.Status);
    }

    [Fact]
    public async Task Al_salir_un_jugador_pagado_se_reembolsa_su_parte()
    {
        var w = new TestWorld();
        w.SeedSplitMatch(Now, 3, 90000m, "org", "b", "c");
        var svc = BuildMatchService(w);

        var init = await svc.PayShareAsync("b", "match1", new PayInput("nequi"));
        var verifier = new StubWebhookVerifier
        {
            Next = new PaymentWebhookEvent("evt-b", $"TX-{init.PaymentId}", init.PaymentId, PaymentWebhookStatus.Approved, "APPROVED"),
        };
        await BuildWebhook(w, verifier).ProcessAsync("{}");

        await svc.LeaveAsync("b", "match1");

        var payment = w.Payments.GetByMatchAndPayer("match1", "b")!;
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.DoesNotContain(w.Matches.GetById("match1")!.Players, p => p.UserId == "b");
    }
}
