namespace CanchasSinteticas.Domain.ValueObjects;

/// <summary>
/// Coordenadas geográficas de una sede (para búsqueda y mapa).
/// </summary>
/// <param name="Latitude">Latitud en grados decimales.</param>
/// <param name="Longitude">Longitud en grados decimales.</param>
public record GeoLocation(double Latitude, double Longitude);
