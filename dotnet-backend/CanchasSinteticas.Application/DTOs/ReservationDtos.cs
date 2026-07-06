using System.Text.Json.Serialization;

namespace CanchasSinteticas.Application.DTOs;

public record CreateReservationInput(
    string UserId,
    string FieldId,
    string Date,
    string StartTime,
    string EndTime);

public record ReservationOutput(
    [property: JsonPropertyName("reservation_id")] string ReservationId,
    [property: JsonPropertyName("field_id")] string FieldId,
    [property: JsonPropertyName("field_name")] string FieldName,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("end_time")] string EndTime,
    [property: JsonPropertyName("status")] string Status);

public record CancelOutput(
    [property: JsonPropertyName("reservation_id")] string ReservationId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("no_show")] bool NoShow);
