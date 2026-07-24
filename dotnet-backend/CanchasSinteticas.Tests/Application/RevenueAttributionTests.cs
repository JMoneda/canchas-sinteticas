using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Tests.Support;

namespace CanchasSinteticas.Tests.Application;

/// <summary>Pruebas de atribución de ingresos: solo cuentan pagos aprobados (FR-028/C4, T062a).</summary>
public class RevenueAttributionTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Solo_los_pagos_aprobados_cuentan_como_ingreso()
    {
        var w = new TestWorld();
        var (_, paid) = w.SeedPendingReservation(Now, price: 100000m);
        paid.StartProcessing("TX", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        paid.MarkApproved("TX", "REF", Now);
        w.Payments.Update(paid);

        // Segunda reserva en la misma cancha con pago pendiente (no debe sumar).
        var date = DateOnly.FromDateTime(Now.AddDays(1));
        var res2 = new Reservation("res2", "court1", "client1", null, null, date,
            new TimeOnly(20, 0), new TimeOnly(21, 0), 90000m, ReservationChannel.Online, Now, pendingPayment: true);
        w.Reservations.Add(res2);
        w.Payments.Add(new Payment("pay2", "res2", 90000m, PaymentMethod.Nequi, PaymentStatus.Pending, null, Now));

        var report = new ReportService(w.Venues, w.Courts, w.Reservations, w.Payments, new FixedClock(Now))
            .GetOwnerReport("owner1", null, null);

        Assert.Equal(100000m, report.TotalRevenue);
        Assert.Equal(1, report.TotalReservations);
    }

    [Fact]
    public void Un_pago_reembolsado_no_cuenta_como_ingreso()
    {
        var w = new TestWorld();
        var (_, payment) = w.SeedPendingReservation(Now, price: 100000m);
        payment.StartProcessing("TX", "url", Now.AddMinutes(15), PaymentMethod.Nequi);
        payment.MarkApproved("TX", "REF", Now);
        payment.RequestRefund();
        payment.ConfirmRefund("R1");
        w.Payments.Update(payment);

        var report = new ReportService(w.Venues, w.Courts, w.Reservations, w.Payments, new FixedClock(Now))
            .GetOwnerReport("owner1", null, null);

        Assert.Equal(0m, report.TotalRevenue);
    }
}
