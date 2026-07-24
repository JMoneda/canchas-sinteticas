using System.Security.Claims;
using CanchasSinteticas.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>
/// Controlador base con utilidades para leer el usuario autenticado del token.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id del usuario autenticado (claim NameIdentifier).</summary>
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new NotAuthorizedError();
}
