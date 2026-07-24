using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Reservas del cliente autenticado.</summary>
[Route("api/reservations")]
[Authorize]
public class ReservationsController(
    ReservationService reservations,
    PaymentService payments,
    ReceiptService receipts) : ApiControllerBase
{
    /// <summary>Crea una reserva para el cliente autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationOutput), 201)]
    [ProducesResponseType(422)]
    public IActionResult Create([FromBody] CreateReservationInput input) =>
        StatusCode(201, reservations.Create(CurrentUserId, input));

    /// <summary>Lista el historial de reservas del cliente.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationOutput>), 200)]
    public IActionResult Mine() => Ok(reservations.ListByClient(CurrentUserId));

    /// <summary>Cancela una reserva del cliente.</summary>
    [HttpDelete("{reservationId}")]
    [ProducesResponseType(typeof(CancelOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(string reservationId) =>
        Ok(await reservations.CancelAsync(CurrentUserId, reservationId));

    /// <summary>Inicia el pago real de una reserva del cliente y devuelve la información de checkout.</summary>
    [HttpPost("{reservationId}/pay")]
    [ProducesResponseType(typeof(PaymentInitiationOutput), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> Pay(string reservationId, [FromBody] PayInput input) =>
        Ok(await payments.PayAsync(CurrentUserId, reservationId, input));

    /// <summary>Descarga el comprobante de una reserva (PDF por defecto; JSON con ?format=json).</summary>
    [HttpGet("{reservationId}/receipt")]
    [ProducesResponseType(typeof(ReceiptOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult Receipt(string reservationId, [FromQuery] string? format)
    {
        var (receipt, pdf) = receipts.GetReservationReceipt(CurrentUserId, reservationId);
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return Ok(ReceiptService.ToOutput(receipt));

        return File(pdf, "application/pdf", $"{receipt.Number}.pdf");
    }
}
