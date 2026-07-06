using System.Text.Json.Serialization;

namespace CanchasSinteticas.Application.DTOs;

public record SlotOutput(
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("end_time")] string EndTime);

public record FieldAvailabilityOutput(
    [property: JsonPropertyName("field_id")] string FieldId,
    [property: JsonPropertyName("field_name")] string FieldName,
    [property: JsonPropertyName("available_slots")] IReadOnlyList<SlotOutput> AvailableSlots);
