using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas del reembolso al cancelar (US4, T053).</summary>
public class RefundTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static (TestWorld World, ReservationService Service, FakePaymentGateway Gateway) Setup(DateTime now)
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(now, price: 100000m);
        payment.StartProcessing("TX", "url", now.AddMinutes(15), PaymentMethod.Nequi);
        payment.MarkApproved("TX", "REF", now);
        w.Payments.Update(payment);
        w.Reservations.GetById("res1")!.Confirm();

        var gateway = new FakePaymentGateway();
        var service = new ReservationService(w.Reservations, w.Venues, w.Courts, w.Prices, w.Blackouts,
            w.Payments, gateway, new StubCredentialsResolver(), new FixedClock(now));
        return (w, service, gateway);
    }

    [Fact]
    public async Task Cancelacion_no_tardia_reembolsa_via_gateway()
    {
        // La reserva es mañana; la ventana de cancelación de la sede es 2h → no es tardía.
        var (w, service, gateway) = Setup(Now);

        var result = await service.CancelAsync("client1", "res1");

        Assert.Equal("refunded", result.RefundStatus);
        Assert.True(gateway.RefundCalled);
        Assert.Equal(PaymentStatus.Refunded, w.Payments.GetByReservation("res1")!.Status);
    }

    [Fact]
    public async Task Cancelacion_tardia_no_reembolsa()
    {
        var w = new TestWorld();
        // Reserva dentro de 1h → dentro de la ventana de 2h ⇒ tardía.
        var now = Now;
        var (reservation, payment) = w.SeedPendingReservation(now, price: 100000m);
        // Forzar que la reserva sea "pronto" no es posible por el helper (mañana); en su lugar validamos
        // la rama tardía con una ventana grande: recreamos la sede con ventana de 48h.
        var venue = w.Venues.GetById("venue1")!;
        venue.CancellationWindowHours = 48;
        w.Venues.Update(venue);

        payment.StartProcessing("TX", "url", now.AddMinutes(15), PaymentMethod.Nequi);
        payment.MarkApproved("TX", "REF", now);
        w.Payments.Update(payment);
        reservation.Confirm();

        var gateway = new FakePaymentGateway();
        var service = new ReservationService(w.Reservations, w.Venues, w.Courts, w.Prices, w.Blackouts,
            w.Payments, gateway, new StubCredentialsResolver(), new FixedClock(now));

        var result = await service.CancelAsync("client1", "res1");

        Assert.Equal("none", result.RefundStatus);
        Assert.False(gateway.RefundCalled);
        Assert.Equal(PaymentStatus.Paid, w.Payments.GetByReservation("res1")!.Status);
    }
}
