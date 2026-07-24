using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Partidos abiertos (matchmaking): publicar, unirse y salir.</summary>
[Route("api/matches")]
public class MatchesController(MatchService matches, ReceiptService receipts) : ApiControllerBase
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

    /// <summary>Quita al usuario autenticado de un partido (con reembolso de su parte si ya pagó).</summary>
    [HttpPost("{matchId}/leave")]
    [Authorize]
    [ProducesResponseType(typeof(MatchOutput), 200)]
    public async Task<IActionResult> Leave(string matchId) => Ok(await matches.LeaveAsync(CurrentUserId, matchId));

    /// <summary>Inicia el pago de la parte del usuario en un partido con pago dividido.</summary>
    [HttpPost("{matchId}/pay-share")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentInitiationOutput), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> PayShare(string matchId, [FromBody] PayInput input) =>
        Ok(await matches.PayShareAsync(CurrentUserId, matchId, input));

    /// <summary>Descarga el comprobante de la parte del usuario en un partido (PDF o ?format=json).</summary>
    [HttpGet("{matchId}/players/me/receipt")]
    [Authorize]
    [ProducesResponseType(typeof(ReceiptOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult ShareReceipt(string matchId, [FromQuery] string? format)
    {
        var (receipt, pdf) = receipts.GetShareReceipt(CurrentUserId, matchId);
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return Ok(ReceiptService.ToOutput(receipt));

        return File(pdf, "application/pdf", $"{receipt.Number}.pdf");
    }
}
