using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Eliminación de bloqueos de cancha del dueño.</summary>
[Route("api/owner/blackouts")]
[Authorize(Roles = "Owner")]
public class OwnerBlackoutsController(BlackoutService blackouts) : ApiControllerBase
{
    /// <summary>Elimina un bloqueo del dueño.</summary>
    [HttpDelete("{blackoutId}")]
    [ProducesResponseType(204)]
    public IActionResult Delete(string blackoutId)
    {
        blackouts.Delete(CurrentUserId, blackoutId);
        return NoContent();
    }
}
