using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Partidos abiertos (matchmaking): publicar, unirse y salir.</summary>
[Route("api/matches")]
public class MatchesController(MatchService matches) : ApiControllerBase
{
    /// <summary>Lista los partidos con cupos, opcionalmente por ciudad.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MatchOutput>), 200)]
    public IActionResult List([FromQuery] string? city) => Ok(matches.ListActive(city));

    /// <summary>Devuelve el detalle de un partido.</summary>
    [HttpGet("{matchId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MatchOutput), 200)]
    [ProducesResponseType(404)]
    public IActionResult Detail(string matchId) => Ok(matches.GetDetail(matchId));

    /// <summary>Abre un partido (crea la reserva del organizador y la publica con cupos).</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(MatchOutput), 201)]
    public IActionResult Open([FromBody] OpenMatchInput input) =>
        StatusCode(201, matches.Open(CurrentUserId, input));

    /// <summary>Une al usuario autenticado a un partido.</summary>
    [HttpPost("{matchId}/join")]
    [Authorize]
    [ProducesResponseType(typeof(MatchOutput), 200)]
    [ProducesResponseType(409)]
    public IActionResult Join(string matchId) => Ok(matches.Join(CurrentUserId, matchId));

    /// <summary>Quita al usuario autenticado de un partido.</summary>
    [HttpPost("{matchId}/leave")]
    [Authorize]
    [ProducesResponseType(typeof(MatchOutput), 200)]
    public IActionResult Leave(string matchId) => Ok(matches.Leave(CurrentUserId, matchId));

    /// <summary>Paga (simulado) la parte del usuario autenticado en un partido con split.</summary>
    [HttpPost("{matchId}/pay")]
    [Authorize]
    [ProducesResponseType(typeof(MatchOutput), 200)]
    public IActionResult Pay(string matchId) => Ok(matches.PayShare(CurrentUserId, matchId));
}
