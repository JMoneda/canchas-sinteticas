using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Inicia el cobro real de una reserva a través del proveedor de pagos y expone el estado del pago.
/// El pago NO se marca aprobado aquí: eso ocurre solo al recibir la confirmación verificada del
/// proveedor por webhook (Regla de Dominio 7).
/// </summary>
public class PaymentService(
    IReservationRepository reservations,
    IPaymentRepository payments,
    ICourtRepository courts,
    IVenueRepository venues,
    IUserRepository users,
    IReceiptRepository receipts,
    IPaymentGateway gateway,
    IPaymentGatewayCredentialsResolver credentials,
    PaymentSettings settings,
    IClock clock)
{
    /// <summary>Inicia el pago de una reserva del cliente y devuelve la información de checkout.</summary>
    public async Task<PaymentInitiationOutput> PayAsync(string clientId, string reservationId, PayInput input)
    {
        var reservation = reservations.GetById(reservationId) ?? throw new NotFoundError();
        if (reservation.ClientId != clientId)
            throw new NotAuthorizedError();

        var payment = payments.GetByReservation(reservationId) ?? throw new NotFoundError();
        if (payment.Status == PaymentStatus.Paid)
            throw new InvalidPaymentTransitionError("Esta reserva ya está pagada.");

        var method = Parsing.ParsePaymentMethod(input.Method);
        var court = courts.GetById(reservation.CourtId) ?? throw new CourtNotFoundError();
        var venue = venues.GetById(court.VenueId) ?? throw new VenueNotFoundError();
        var customerEmail = users.GetById(clientId)?.Email;
        var expiresAt = clock.Now.AddMinutes(settings.ExpiryMinutes);

        GatewayTransactionResult tx;
        try
        {
            tx = await gateway.CreateTransactionAsync(new CreateTransactionRequest(
                PaymentId: payment.Id,
                Reference: payment.Id,
                Amount: payment.Amount,
                Method: method,
                CustomerEmail: customerEmail,
                ReturnUrl: input.ReturnUrl,
                Credentials: credentials.Resolve(venue)));
        }
        catch (PaymentGatewayError)
        {
            // El proveedor no respondió: el pago falla pero la reserva sigue reteniendo la franja
            // hasta que expire por el sweeper; el cliente puede reintentar.
            payment.Fail("gateway_error_on_create");
            payments.Update(payment);
            throw;
        }

        payment.StartProcessing(tx.TransactionId, tx.CheckoutUrl, expiresAt, method);
        payments.Update(payment);

        return new PaymentInitiationOutput(
            payment.Id,
            reservationId,
            payment.Status.ToString(),
            payment.Amount,
            method.ToString(),
            payment.CheckoutUrl,
            expiresAt.ToString("s"));
    }

    /// <summary>Consulta el estado de un pago; accesible por el titular o por el dueño de la sede.</summary>
    public PaymentStatusOutput GetStatus(string userId, string paymentId)
    {
        var payment = payments.GetById(paymentId) ?? throw new NotFoundError();
        var reservation = reservations.GetById(payment.ReservationId) ?? throw new NotFoundError();

        var isClient = reservation.ClientId == userId || payment.PayerUserId == userId;
        var isOwner = false;
        var court = courts.GetById(reservation.CourtId);
        if (court is not null)
        {
            var venue = venues.GetById(court.VenueId);
            isOwner = venue is not null && venue.OwnerId == userId;
        }

        if (!isClient && !isOwner)
            throw new NotAuthorizedError();

        return new PaymentStatusOutput(
            payment.Id,
            payment.ReservationId,
            payment.Status.ToString(),
            payment.Amount,
            payment.Method.ToString(),
            payment.GatewayReference,
            payment.PaidAt?.ToString("s"),
            HasReceipt: receipts.GetByPayment(payment.Id) is not null);
    }
}
