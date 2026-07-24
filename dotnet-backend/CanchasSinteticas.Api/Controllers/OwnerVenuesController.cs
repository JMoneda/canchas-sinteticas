using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Gestión de sedes y sus canchas por parte del dueño.</summary>
[Route("api/owner/venues")]
[Authorize(Roles = "Owner")]
public class OwnerVenuesController(
    VenueService venues,
    CourtService courts,
    VenuePaymentConfigService paymentConfig) : ApiControllerBase
{
    /// <summary>Lista las sedes del dueño autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueDetailOutput>), 200)]
    public IActionResult List() => Ok(venues.GetByOwner(CurrentUserId));

    /// <summary>Crea una sede.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VenueDetailOutput), 201)]
    public IActionResult Create([FromBody] CreateVenueInput input) =>
        StatusCode(201, venues.Create(CurrentUserId, input));

    /// <summary>Actualiza una sede del dueño.</summary>
    [HttpPut("{venueId}")]
    [ProducesResponseType(typeof(VenueDetailOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult Update(string venueId, [FromBody] UpdateVenueInput input) =>
        Ok(venues.Update(CurrentUserId, venueId, input));

    /// <summary>Elimina una sede del dueño y sus canchas.</summary>
    [HttpDelete("{venueId}")]
    [ProducesResponseType(204)]
    public IActionResult Delete(string venueId)
    {
        venues.Delete(CurrentUserId, venueId);
        return NoContent();
    }

    /// <summary>Lista las canchas de una sede del dueño.</summary>
    [HttpGet("{venueId}/courts")]
    [ProducesResponseType(typeof(IReadOnlyList<CourtOutput>), 200)]
    public IActionResult Courts(string venueId) => Ok(courts.GetByVenue(CurrentUserId, venueId));

    /// <summary>Crea una cancha en una sede del dueño.</summary>
    [HttpPost("{venueId}/courts")]
    [ProducesResponseType(typeof(CourtOutput), 201)]
    public IActionResult CreateCourt(string venueId, [FromBody] CreateCourtInput input) =>
        StatusCode(201, courts.Create(CurrentUserId, venueId, input));

    /// <summary>Obtiene el modelo de recaudo de una sede del dueño.</summary>
    [HttpGet("{venueId}/payment-config")]
    [ProducesResponseType(typeof(VenuePaymentConfigOutput), 200)]
    public IActionResult GetPaymentConfig(string venueId) =>
        Ok(paymentConfig.Get(CurrentUserId, venueId));

    /// <summary>Define el modelo de recaudo de una sede (marketplace o cuenta directa).</summary>
    [HttpPut("{venueId}/payment-config")]
    [ProducesResponseType(typeof(VenuePaymentConfigOutput), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public IActionResult SetPaymentConfig(string venueId, [FromBody] VenuePaymentConfigInput input) =>
        Ok(paymentConfig.Set(CurrentUserId, venueId, input));
}
