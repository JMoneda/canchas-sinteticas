using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Agenda de reservas del dueño y creación de reservas manuales.</summary>
[Route("api/owner/reservations")]
[Authorize(Roles = "Owner")]
public class OwnerReservationsController(ReservationService reservations) : ApiControllerBase
{
    /// <summary>Lista las reservas de las canchas del dueño, opcionalmente por fecha (yyyy-MM-dd).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationOutput>), 200)]
    public IActionResult List([FromQuery] string? date)
    {
        return Ok(reservations.ListByOwner(CurrentUserId, Parsing.ParseDateOrNull(date)));
    }

    /// <summary>Crea una reserva manual (walk-in / teléfono) en una cancha del dueño.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationOutput), 201)]
    public IActionResult CreateManual([FromBody] ManualReservationInput input) =>
        StatusCode(201, reservations.CreateManual(CurrentUserId, input));
}
