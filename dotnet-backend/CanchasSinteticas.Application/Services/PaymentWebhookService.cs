using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Procesa los eventos de webhook del proveedor: verifica su autenticidad, aplica idempotencia y
/// transiciona el pago y la reserva. Es la única vía por la que un pago pasa a aprobado (Regla 7).
/// </summary>
public class PaymentWebhookService(
    IPaymentWebhookVerifier verifier,
    IPaymentRepository payments,
    IReservationRepository reservations,
    ICourtRepository courts,
    IMatchRepository matches,
    IProcessedWebhookEventRepository processedEvents,
    IPaymentGateway gateway,
    IPaymentGatewayCredentialsResolver credentials,
    IVenueRepository venues,
    INotificationSender notifier,
    ReceiptService receipts,
    IClock clock)
{
    /// <summary>Procesa el cuerpo crudo de un webhook. Devuelve true si se aceptó (verificado).</summary>
    public async Task<bool> ProcessAsync(string rawBody)
    {
        var evt = verifier.VerifyAndParse(rawBody);
        if (evt is null)
            return false; // firma inválida o cuerpo no interpretable: no se cambia ningún estado (FR-005)

        if (processedEvents.Exists(evt.EventId))
            return true; // idempotencia: evento ya procesado (FR-006)

        var payment = payments.GetById(evt.Reference)
            ?? payments.GetByGatewayTransactionId(evt.TransactionId);

        if (payment is not null)
            await ApplyAsync(payment, evt);

        processedEvents.Add(new ProcessedWebhookEvent(evt.EventId, evt.TransactionId, clock.Now));
        return true;
    }

    private async Task ApplyAsync(Payment payment, PaymentWebhookEvent evt)
    {
        switch (evt.Status)
        {
            case PaymentWebhookStatus.Approved:
                await ApproveAsync(payment, evt);
                break;

            case PaymentWebhookStatus.Declined or PaymentWebhookStatus.Error:
                if (payment.Status is PaymentStatus.Pending or PaymentStatus.Processing)
                {
                    payment.MarkRejected(evt.RawStatus);
                    payments.Update(payment);
                    ReleaseReservation(payment);
                    await NotifyAsync(payment, PaymentNotificationKind.Rejected);
                }

                break;

            case PaymentWebhookStatus.Voided:
                if (payment.Status == PaymentStatus.RefundRequested)
                {
                    payment.ConfirmRefund(evt.TransactionId);
                    payments.Update(payment);
                    await NotifyAsync(payment, PaymentNotificationKind.Refunded);
                }

                break;

            case PaymentWebhookStatus.Pending:
            default:
                break;
        }
    }

    private async Task ApproveAsync(Payment payment, PaymentWebhookEvent evt)
    {
        // Conciliación de aprobación tardía tras expirar (C2): si el pago ya expiró, se reactiva.
        // Si la franja sigue retenida por la reserva se reconfirma; si ya fue liberada, se reembolsa
        // automáticamente para no cobrar sin cupo ni provocar doble reserva.
        if (payment.Status == PaymentStatus.Expired)
        {
            var res = reservations.GetById(payment.ReservationId);
            var slotStillHeld = res is not null && res.IsActive;

            payment.Reactivate();
            payment.MarkApproved(evt.TransactionId, evt.Reference, clock.Now);
            payments.Update(payment);

            if (slotStillHeld)
            {
                receipts.GenerateFor(payment);
                ConfirmReservation(payment);
                await NotifyAsync(payment, PaymentNotificationKind.Approved);
            }
            else
            {
                var refunded = await TryRefundAsync(payment);
                await NotifyAsync(payment, refunded ? PaymentNotificationKind.Refunded : PaymentNotificationKind.Approved);
            }

            return;
        }

        if (payment.Status == PaymentStatus.Paid)
            return; // idempotente

        payment.MarkApproved(evt.TransactionId, evt.Reference, clock.Now);
        payments.Update(payment);
        receipts.GenerateFor(payment);
        ConfirmReservation(payment);
        await NotifyAsync(payment, PaymentNotificationKind.Approved);
    }

    private async Task<bool> TryRefundAsync(Payment payment)
    {
        var venue = VenueOf(payment);
        if (venue is null)
            return false;

        payment.RequestRefund();
        payments.Update(payment);
        try
        {
            var result = await gateway.RefundAsync(
                payment.GatewayTransactionId ?? payment.Id,
                payment.Amount,
                credentials.Resolve(venue));
            payment.ConfirmRefund(result.RefundReference);
            payments.Update(payment);
            return true;
        }
        catch (Exception)
        {
            // El reembolso queda solicitado; se reconciliará por el webhook de reembolso.
            return true;
        }
    }

    private void ConfirmReservation(Payment payment)
    {
        if (payment.IsShare)
        {
            ConfirmShare(payment);
            return;
        }

        var reservation = reservations.GetById(payment.ReservationId);
        if (reservation is null)
            return;

        reservation.Confirm();
        reservations.Update(reservation);
    }

    private void ConfirmShare(Payment payment)
    {
        var match = matches.GetById(payment.MatchId!);
        if (match is null || payment.PayerUserId is null)
            return;

        match.ConfirmSharePayment(payment.PayerUserId, payment.Id);
        matches.Update(match);

        // Cuando las partes cubren el total, se confirma la reserva del partido.
        if (match.IsFullyCollected)
        {
            var reservation = reservations.GetById(match.ReservationId);
            if (reservation is not null)
            {
                reservation.Confirm();
                reservations.Update(reservation);
            }
        }
    }

    private void ReleaseReservation(Payment payment)
    {
        if (payment.IsShare)
            return;

        var reservation = reservations.GetById(payment.ReservationId);
        if (reservation is null || reservation.Status != ReservationStatus.Pending)
            return;

        reservation.Cancel(isLate: false);
        reservations.Update(reservation);
    }

    private Venue? VenueOf(Payment payment)
    {
        var reservation = reservations.GetById(payment.ReservationId);
        var court = reservation is null ? null : courts.GetById(reservation.CourtId);
        return court is null ? null : venues.GetById(court.VenueId);
    }

    private async Task NotifyAsync(Payment payment, PaymentNotificationKind kind)
    {
        var reservation = reservations.GetById(payment.ReservationId);
        var userId = payment.PayerUserId ?? reservation?.ClientId;
        if (string.IsNullOrEmpty(userId))
            return;

        await notifier.NotifyAsync(new PaymentNotification(
            userId, kind, payment.ReservationId, payment.Amount, payment.GatewayReference));
    }
}
