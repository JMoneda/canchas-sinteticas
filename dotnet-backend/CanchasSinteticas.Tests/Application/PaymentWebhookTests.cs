using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas del procesamiento del webhook (US1, T026): aprobación, idempotencia y firma inválida.</summary>
public class PaymentWebhookTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static (PaymentWebhookService Service, StubWebhookVerifier Verifier, RecordingNotifier Notifier)
        Build(TestWorld w)
    {
        var verifier = new StubWebhookVerifier();
        var notifier = new RecordingNotifier();
        var clock = new FixedClock(Now);
        var service = new PaymentWebhookService(
            verifier, w.Payments, w.Reservations, w.Courts, w.Matches, w.ProcessedEvents,
            new FakePaymentGateway(), new StubCredentialsResolver(), w.Venues, notifier,
            w.BuildReceiptService(clock), clock);
        return (service, verifier, notifier);
    }

    private static PaymentWebhookEvent Approved(string paymentId, string eventId = "evt-1") =>
        new(eventId, $"TX-{paymentId}", paymentId, PaymentWebhookStatus.Approved, "APPROVED");

    [Fact]
    public async Task Webhook_aprobado_confirma_reserva_y_marca_pago_pagado()
    {
        var w = new TestWorld();
        var (reservation, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        w.Payments.Update(payment);

        var (service, verifier, notifier) = Build(w);
        verifier.Next = Approved(payment.Id);

        var accepted = await service.ProcessAsync("{...}");

        Assert.True(accepted);
        Assert.Equal(PaymentStatus.Paid, w.Payments.GetById(payment.Id)!.Status);
        Assert.Equal(ReservationStatus.Confirmed, w.Reservations.GetById(reservation.Id)!.Status);
        Assert.Contains(notifier.Sent, n => n.Kind == PaymentNotificationKind.Approved);
    }

    [Fact]
    public async Task Webhook_repetido_es_idempotente()
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        w.Payments.Update(payment);

        var (service, verifier, notifier) = Build(w);
        verifier.Next = Approved(payment.Id, "evt-dup");

        await service.ProcessAsync("{...}");
        await service.ProcessAsync("{...}"); // mismo evento reenviado

        Assert.Equal(PaymentStatus.Paid, w.Payments.GetById(payment.Id)!.Status);
        // Solo una notificación de aprobación (no se reprocesa).
        Assert.Single(notifier.Sent, n => n.Kind == PaymentNotificationKind.Approved);
    }

    [Fact]
    public async Task Webhook_con_firma_invalida_no_cambia_estado()
    {
        var w = new TestWorld();
        var (reservation, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        w.Payments.Update(payment);

        var (service, verifier, _) = Build(w);
        verifier.Next = null; // firma inválida / cuerpo no interpretable

        var accepted = await service.ProcessAsync("cuerpo-falsificado");

        Assert.False(accepted);
        Assert.Equal(PaymentStatus.Processing, w.Payments.GetById(payment.Id)!.Status);
        Assert.Equal(ReservationStatus.Pending, w.Reservations.GetById(reservation.Id)!.Status);
    }

    [Fact]
    public async Task Webhook_rechazado_libera_la_franja()
    {
        var w = new TestWorld();
        var (reservation, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Pse);
        w.Payments.Update(payment);

        var (service, verifier, _) = Build(w);
        verifier.Next = new PaymentWebhookEvent("evt-rej", $"TX-{payment.Id}", payment.Id, PaymentWebhookStatus.Declined, "DECLINED");

        await service.ProcessAsync("{...}");

        Assert.Equal(PaymentStatus.Rejected, w.Payments.GetById(payment.Id)!.Status);
        Assert.Equal(ReservationStatus.Cancelled, w.Reservations.GetById(reservation.Id)!.Status);
    }
}
