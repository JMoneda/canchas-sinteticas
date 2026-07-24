using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Gestión de una cancha concreta: datos, tarifas y bloqueos.</summary>
[Route("api/owner/courts")]
[Authorize(Roles = "Owner")]
public class OwnerCourtsController(
    CourtService courts,
    BlackoutService blackouts) : ApiControllerBase
{
    /// <summary>Actualiza los datos de una cancha del dueño.</summary>
    [HttpPut("{courtId}")]
    [ProducesResponseType(typeof(CourtOutput), 200)]
    public IActionResult Update(string courtId, [FromBody] UpdateCourtInput input) =>
        Ok(courts.Update(CurrentUserId, courtId, input));

    /// <summary>Elimina una cancha del dueño.</summary>
    [HttpDelete("{courtId}")]
    [ProducesResponseType(204)]
    public IActionResult Delete(string courtId)
    {
        courts.Delete(CurrentUserId, courtId);
        return NoContent();
    }

    /// <summary>Reemplaza las tarifas por franja de una cancha.</summary>
    [HttpPut("{courtId}/prices")]
    [ProducesResponseType(typeof(CourtOutput), 200)]
    public IActionResult SetPrices(string courtId, [FromBody] SetPricesInput input) =>
        Ok(courts.SetPrices(CurrentUserId, courtId, input));

    /// <summary>Lista los bloqueos de una cancha del dueño.</summary>
    [HttpGet("{courtId}/blackouts")]
    [ProducesResponseType(typeof(IReadOnlyList<BlackoutOutput>), 200)]
    public IActionResult Blackouts(string courtId) => Ok(blackouts.ListByCourt(CurrentUserId, courtId));

    /// <summary>Crea un bloqueo en una cancha del dueño.</summary>
    [HttpPost("{courtId}/blackouts")]
    [ProducesResponseType(typeof(BlackoutOutput), 201)]
    public IActionResult CreateBlackout(string courtId, [FromBody] CreateBlackoutInput input) =>
        StatusCode(201, blackouts.Create(CurrentUserId, courtId, input));
}
