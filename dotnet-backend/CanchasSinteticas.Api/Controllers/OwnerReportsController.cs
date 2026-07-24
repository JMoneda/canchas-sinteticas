using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanchasSinteticas.Api.Controllers;

/// <summary>Reportes de ocupación e ingresos del dueño.</summary>
[Route("api/owner/reports")]
[Authorize(Roles = "Owner")]
public class OwnerReportsController(ReportService reports) : ApiControllerBase
{
    /// <summary>Devuelve el reporte del dueño en el rango indicado (por defecto, últimos 30 días).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(OwnerReportOutput), 200)]
    public IActionResult Get([FromQuery] string? from, [FromQuery] string? to)
    {
        return Ok(reports.GetOwnerReport(CurrentUserId, Parsing.ParseDateOrNull(from), Parsing.ParseDateOrNull(to)));
    }
}
