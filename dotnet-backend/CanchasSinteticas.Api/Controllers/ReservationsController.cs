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
    PaymentService payments) : ApiControllerBase
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
    public IActionResult Cancel(string reservationId) =>
        Ok(reservations.Cancel(CurrentUserId, reservationId));

    /// <summary>Paga (simulado) una reserva del cliente.</summary>
    [HttpPost("{reservationId}/pay")]
    [ProducesResponseType(typeof(PaymentOutput), 200)]
    [ProducesResponseType(404)]
    public IActionResult Pay(string reservationId, [FromBody] PayInput input) =>
        Ok(payments.Pay(CurrentUserId, reservationId, input));
}
