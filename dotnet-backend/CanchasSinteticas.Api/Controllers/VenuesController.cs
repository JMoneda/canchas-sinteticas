using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Catálogo público de sedes para el marketplace del cliente.</summary>
[Route("api/venues")]
[AllowAnonymous]
public class VenuesController(VenueService venues) : ApiControllerBase
{
    /// <summary>Busca sedes activas, opcionalmente filtradas por ciudad.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueSummaryOutput>), 200)]
    public IActionResult Search([FromQuery] string? city) => Ok(venues.Search(city));

    /// <summary>Devuelve el detalle de una sede con sus canchas.</summary>
    [HttpGet("{venueId}")]
    [ProducesResponseType(typeof(VenueDetailOutput), 200)]
    [ProducesResponseType(404)]
    public IActionResult Detail(string venueId) => Ok(venues.GetDetail(venueId));
}
