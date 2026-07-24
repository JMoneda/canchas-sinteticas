using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas del inicio de pago de una reserva (US1, T025 y T027a).</summary>
public class PaymentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static PaymentService Build(TestWorld w, FakePaymentGateway gateway) =>
        new(w.Reservations, w.Payments, w.Courts, w.Venues, w.Users, w.Receipts, gateway,
            new StubCredentialsResolver(), new PaymentSettings(15), new FixedClock(Now));

    [Fact]
    public async Task Pay_crea_transaccion_y_deja_el_pago_en_processing()
    {
        var w = new TestWorld();
        var (reservation, _) = w.SeedPendingReservation(Now);
        var service = Build(w, new FakePaymentGateway());

        var result = await service.PayAsync("client1", reservation.Id, new PayInput("nequi"));

        Assert.Equal("Processing", result.Status);
        Assert.False(string.IsNullOrEmpty(result.CheckoutUrl));

        var payment = w.Payments.GetByReservation(reservation.Id)!;
        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal(PaymentMethod.Nequi, payment.Method);

        // Regla 7: aún no está pagado; la reserva sigue pendiente.
        Assert.NotEqual(PaymentStatus.Paid, payment.Status);
        Assert.Equal(ReservationStatus.Pending, w.Reservations.GetById(reservation.Id)!.Status);
    }

    [Fact]
    public async Task Pay_con_proveedor_caido_falla_el_pago_y_no_bloquea_la_franja()
    {
        var w = new TestWorld();
        var (reservation, _) = w.SeedPendingReservation(Now);
        var gateway = new FakePaymentGateway { ThrowOnCreate = true };
        var service = Build(w, gateway);

        await Assert.ThrowsAsync<PaymentGatewayError>(
            () => service.PayAsync("client1", reservation.Id, new PayInput("pse")));

        var payment = w.Payments.GetByReservation(reservation.Id)!;
        Assert.Equal(PaymentStatus.Failed, payment.Status);

        // La reserva sigue pendiente (retiene la franja) hasta que expire; no queda confirmada ni bloqueada indefinidamente.
        Assert.Equal(ReservationStatus.Pending, w.Reservations.GetById(reservation.Id)!.Status);
    }

    [Fact]
    public async Task Pay_de_otro_cliente_no_autorizado()
    {
        var w = new TestWorld();
        var (reservation, _) = w.SeedPendingReservation(Now);
        var service = Build(w, new FakePaymentGateway());

        await Assert.ThrowsAsync<NotAuthorizedError>(
            () => service.PayAsync("otro", reservation.Id, new PayInput("card")));
    }
}
