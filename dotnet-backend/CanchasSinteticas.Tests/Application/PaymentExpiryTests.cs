using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas de la expiración de pagos y liberación de franja (US1, T027).</summary>
public class PaymentExpiryTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SweepExpired_expira_pago_vencido_y_libera_la_franja()
    {
        var w = new TestWorld();
        var (reservation, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        w.Payments.Update(payment);

        var clock = new FixedClock(Now.AddMinutes(16)); // ya venció
        var service = new PaymentExpiryService(w.Payments, w.Reservations, clock);

        var expired = service.SweepExpired();

        Assert.Equal(1, expired);
        Assert.Equal(PaymentStatus.Expired, w.Payments.GetById(payment.Id)!.Status);
        Assert.Equal(ReservationStatus.Cancelled, w.Reservations.GetById(reservation.Id)!.Status);
    }

    [Fact]
    public void SweepExpired_no_toca_pagos_vigentes()
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(Now);
        payment.StartProcessing($"TX-{payment.Id}", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        w.Payments.Update(payment);

        var clock = new FixedClock(Now.AddMinutes(5)); // aún vigente
        var service = new PaymentExpiryService(w.Payments, w.Reservations, clock);

        Assert.Equal(0, service.SweepExpired());
        Assert.Equal(PaymentStatus.Processing, w.Payments.GetById(payment.Id)!.Status);
    }
}
