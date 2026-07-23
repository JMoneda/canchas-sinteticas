using System.Text.Json.Serialization;

namespace CanchasSinteticas.Application.DTOs;

/// <summary>
/// Representa un slot de tiempo disponible para una cancha.
/// </summary>
/// <param name="StartTime">Hora de inicio del slot en formato ISO 8601.</param>
/// <param name="EndTime">Hora de fin del slot en formato ISO 8601.</param>
public record SlotOutput(
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("end_time")] string EndTime);

/// <summary>
/// Información de disponibilidad de una cancha con sus slots disponibles.
/// </summary>
/// <param name="FieldId">Identificador único de la cancha.</param>
/// <param name="FieldName">Nombre de la cancha.</param>
/// <param name="AvailableSlots">Lista de slots de tiempo disponibles.</param>
public record FieldAvailabilityOutput(
    [property: JsonPropertyName("field_id")] string FieldId,
    [property: JsonPropertyName("field_name")] string FieldName,
    [property: JsonPropertyName("available_slots")] IReadOnlyList<SlotOutput> AvailableSlots);
