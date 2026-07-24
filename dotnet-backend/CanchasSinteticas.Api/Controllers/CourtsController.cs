using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Disponibilidad pública de canchas.</summary>
[Route("api/courts")]
[AllowAnonymous]
public class CourtsController(AvailabilityService availability) : ApiControllerBase
{
    /// <summary>Devuelve los slots de una cancha para una fecha (yyyy-MM-dd).</summary>
    [HttpGet("{courtId}/availability")]
    [ProducesResponseType(typeof(CourtAvailabilityOutput), 200)]
    [ProducesResponseType(404)]
    public IActionResult Availability(string courtId, [FromQuery] string date)
    {
        var parsed = Parsing.ParseDate(date);
        return Ok(availability.GetCourtAvailability(courtId, parsed));
    }
}
