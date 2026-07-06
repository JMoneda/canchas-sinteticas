using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

[ApiController]
[Route("api/fields")]
[Produces("application/json")]
public class FieldsController(ListAvailableSlotsUseCase listSlots, ILogger<FieldsController> logger) : ControllerBase
{
    /// <summary>Disponibilidad de canchas</summary>
    /// <remarks>Devuelve las 3 canchas con sus turnos libres (bloques de 1 hora) para la fecha indicada.</remarks>
    /// <param name="date">Fecha en formato YYYY-MM-DD</param>
    [HttpGet("availability", Name = "GetFieldAvailability")]
    [ProducesResponseType(typeof(IReadOnlyList<FieldAvailabilityOutput>), 200)]
    [ProducesResponseType(400)]
    public IActionResult GetAvailability([FromQuery] string? date)
    {
        // TEMP: diagnose binding from external agents (remove after confirming received value)
        logger.LogInformation("[GetAvailability] raw 'date' param received: {Date}", date ?? "<null>");

        var now = DateTime.Now;

        if (!DateOnly.TryParse(date, out var queryDate))
            return BadRequest(new { error_type = "INVALID_DATE", message = "Date must be in YYYY-MM-DD format." });

        if (queryDate < DateOnly.FromDateTime(now))
            return BadRequest(new { error_type = "INVALID_DATE", message = "Cannot query availability for past dates." });

        var result = listSlots.Execute(queryDate, now);
        return Ok(result);
    }
}
