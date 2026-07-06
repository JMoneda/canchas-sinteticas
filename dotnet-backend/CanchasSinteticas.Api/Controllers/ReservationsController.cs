using System.Text.Json.Serialization;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CanchasSinteticas.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Produces("application/json")]
public class ReservationsController(
    CreateReservationUseCase createReservation,
    ListReservationsUseCase listReservations,
    CancelReservationUseCase cancelReservation,
    ILogger<ReservationsController> logger) : ControllerBase
{
    /// <summary>Crear reserva</summary>
    /// <remarks>Crea una reserva para un turno específico. Valida solapamiento, anticipación mínima y límite de reservas activas.</remarks>
    [HttpPost(Name = "CreateReservation")]
    [ProducesResponseType(typeof(ReservationOutput), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public IActionResult Create(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateReservationRequest? body)
    {
        // TEMP: diagnose binding from external agents (remove after confirming received values)
        var rawBody = HttpContext.Items["RawRequestBody"] as string ?? "<not captured>";
        logger.LogInformation(
            "[CreateReservation] Content-Type: {ContentType} | Content-Length: {ContentLength} | raw body: {RawBody}",
            Request.ContentType ?? "<null>",
            Request.ContentLength?.ToString() ?? "<null>",
            string.IsNullOrWhiteSpace(rawBody) ? "<empty>" : rawBody);

        if (body is null)
        {
            return BadRequest(new
            {
                error_type = "MISSING_BODY",
                message = "Request body is required.",
                missing_fields = new[] { "user_id", "field_id", "date", "start_time", "end_time" }
            });
        }

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(body.UserId))   missingFields.Add("user_id");
        if (string.IsNullOrWhiteSpace(body.FieldId))  missingFields.Add("field_id");
        if (string.IsNullOrWhiteSpace(body.Date))     missingFields.Add("date");
        if (string.IsNullOrWhiteSpace(body.StartTime)) missingFields.Add("start_time");
        if (string.IsNullOrWhiteSpace(body.EndTime))  missingFields.Add("end_time");

        if (missingFields.Count > 0)
        {
            return BadRequest(new
            {
                error_type = "MISSING_FIELDS",
                message = "One or more required fields are missing or empty.",
                missing_fields = missingFields
            });
        }

        var input = new CreateReservationInput(
            body.UserId, body.FieldId, body.Date, body.StartTime, body.EndTime);
        var result = createReservation.Execute(input, DateTime.Now);
        return StatusCode(201, result);
    }

    /// <summary>Listar reservas del usuario</summary>
    /// <remarks>Devuelve las reservas activas y próximas del usuario indicado.</remarks>
    /// <param name="user_id">Identificador del usuario</param>
    [HttpGet(Name = "ListReservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationOutput>), 200)]
    [ProducesResponseType(400)]
    public IActionResult List([FromQuery(Name = "user_id")] string? user_id)
    {
        // TEMP: diagnose binding from external agents (remove after confirming received value)
        logger.LogInformation("[ListReservations] raw 'user_id' param received: {UserId}", user_id ?? "<null>");

        if (string.IsNullOrWhiteSpace(user_id))
            return BadRequest(new { error_type = "MISSING_PARAM", message = "The 'user_id' query parameter is required." });

        var result = listReservations.Execute(user_id, DateTime.Now);
        return Ok(result);
    }

    /// <summary>Cancelar reserva</summary>
    /// <remarks>Cancela una reserva. Si se cancela con menos de 2 horas de anticipación, queda registrado como no-show.</remarks>
    /// <param name="reservationId">ID de la reserva a cancelar</param>
    [HttpDelete("{reservationId}", Name = "CancelReservation")]
    [ProducesResponseType(typeof(CancelOutput), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult Cancel(string reservationId, [FromBody] CancelReservationRequest body)
    {
        var result = cancelReservation.Execute(reservationId, body.UserId, DateTime.Now);
        return Ok(result);
    }
}

public record CreateReservationRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("field_id")] string FieldId,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("end_time")] string EndTime);

public record CancelReservationRequest(
    [property: JsonPropertyName("user_id")] string UserId);
