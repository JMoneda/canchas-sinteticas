using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Tests.Domain;

/// <summary>
/// Pruebas de las transiciones de estado de <see cref="Payment"/>. Cubren la Regla de Dominio 7:
/// un pago solo puede marcarse aprobado (Paid) tras la confirmación del proveedor y nunca de forma
/// optimista, además de la idempotencia de estados terminales.
/// </summary>
public class PaymentStateMachineTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static Payment NewPending() =>
        new("pay1", "res1", 120000m, PaymentMethod.OnlineGateway, PaymentStatus.Pending, null, Now);

    [Fact]
    public void StartProcessing_desde_pending_pasa_a_processing()
    {
        var p = NewPending();
        p.StartProcessing("TX1", "https://checkout", Now.AddMinutes(15), PaymentMethod.Nequi);

        Assert.Equal(PaymentStatus.Processing, p.Status);
        Assert.Equal("TX1", p.GatewayTransactionId);
        Assert.Equal(PaymentMethod.Nequi, p.Method);
        Assert.Equal(Now.AddMinutes(15), p.ExpiresAt);
    }

    [Fact]
    public void MarkApproved_desde_processing_pasa_a_paid_con_referencia()
    {
        var p = NewPending();
        p.StartProcessing("TX1", "https://checkout", Now.AddMinutes(15), PaymentMethod.Nequi);
        p.MarkApproved("TX1", "WOMPI-REF-1", Now.AddMinutes(2));

        Assert.Equal(PaymentStatus.Paid, p.Status);
        Assert.Equal("WOMPI-REF-1", p.GatewayReference);
        Assert.Equal(Now.AddMinutes(2), p.PaidAt);
    }

    [Fact]
    public void MarkApproved_es_idempotente_no_cambia_paidAt_ni_lanza()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Nequi);
        p.MarkApproved("TX1", "WOMPI-REF-1", Now.AddMinutes(2));

        // Reaplicar el mismo evento no debe fallar ni mover PaidAt (idempotencia FR-006/SC-003).
        p.MarkApproved("TX1", "WOMPI-REF-1", Now.AddMinutes(9));

        Assert.Equal(PaymentStatus.Paid, p.Status);
        Assert.Equal(Now.AddMinutes(2), p.PaidAt);
    }

    [Fact]
    public void MarkRejected_desde_processing_pasa_a_rejected()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Pse);
        p.MarkRejected("DECLINED");

        Assert.Equal(PaymentStatus.Rejected, p.Status);
    }

    [Fact]
    public void MarkExpired_desde_processing_pasa_a_expired()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Pse);
        p.MarkExpired();

        Assert.Equal(PaymentStatus.Expired, p.Status);
    }

    [Fact]
    public void MarkExpired_sobre_pago_pagado_lanza()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Pse);
        p.MarkApproved("TX1", "REF", Now);

        Assert.Throws<InvalidPaymentTransitionError>(() => p.MarkExpired());
    }

    [Fact]
    public void MarkApproved_desde_estado_no_procesable_lanza()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Pse);
        p.MarkExpired();

        // Regla 7 + integridad: no se puede aprobar un pago expirado sin reactivarlo primero.
        Assert.Throws<InvalidPaymentTransitionError>(() => p.MarkApproved("TX1", "REF", Now));
    }

    [Fact]
    public void Reactivate_permite_reconfirmar_una_aprobacion_tardia()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Pse);
        p.MarkExpired();

        p.Reactivate();
        p.MarkApproved("TX1", "REF", Now.AddMinutes(20));

        Assert.Equal(PaymentStatus.Paid, p.Status);
    }

    [Fact]
    public void Refund_desde_paid_sigue_requested_luego_refunded()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Card);
        p.MarkApproved("TX1", "REF", Now);

        p.RequestRefund();
        Assert.Equal(PaymentStatus.RefundRequested, p.Status);

        p.ConfirmRefund("REFUND-1");
        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.Equal("REFUND-1", p.RefundReference);
    }

    [Fact]
    public void FailRefund_revierte_a_paid()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Card);
        p.MarkApproved("TX1", "REF", Now);
        p.RequestRefund();

        p.FailRefund();

        Assert.Equal(PaymentStatus.Paid, p.Status);
    }

    [Fact]
    public void RequestRefund_sin_estar_pagado_lanza()
    {
        var p = NewPending();
        Assert.Throws<InvalidPaymentTransitionError>(() => p.RequestRefund());
    }

    [Fact]
    public void Fail_desde_processing_pasa_a_failed()
    {
        var p = NewPending();
        p.StartProcessing("TX1", null, Now.AddMinutes(15), PaymentMethod.Nequi);
        p.Fail("timeout");

        Assert.Equal(PaymentStatus.Failed, p.Status);
    }

    [Fact]
    public void Share_marca_matchId_y_payer()
    {
        var share = new Payment(
            "pay-share", "res1", 30000m, PaymentMethod.Nequi, PaymentStatus.Pending, null, Now,
            matchId: "match1", payerUserId: "user5");

        Assert.True(share.IsShare);
        Assert.Equal("match1", share.MatchId);
        Assert.Equal("user5", share.PayerUserId);
    }
}
