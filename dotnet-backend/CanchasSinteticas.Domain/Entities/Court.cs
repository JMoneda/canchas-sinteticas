using CanchasSinteticas.Domain.Enums;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Cancha individual que pertenece a una sede.
/// </summary>
public class Court
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Sede a la que pertenece.</summary>
    public string VenueId { get; }

    /// <summary>Nombre o número de la cancha.</summary>
    public string Name { get; set; }

    /// <summary>Modalidad (F5, F7, F11...).</summary>
    public CourtType Type { get; set; }

    /// <summary>Tipo de superficie (sintética, grama natural...).</summary>
    public string Surface { get; set; }

    /// <summary>Indica si la cancha es techada.</summary>
    public bool Covered { get; set; }

    /// <summary>Duración de cada bloque reservable, en minutos.</summary>
    public int SlotDurationMinutes { get; set; }

    /// <summary>Indica si la cancha está activa.</summary>
    public bool Active { get; set; }

    /// <summary>Crea una cancha.</summary>
    public Court(
        string id,
        string venueId,
        string name,
        CourtType type,
        string surface,
        bool covered,
        int slotDurationMinutes,
        bool active)
    {
        Id = id;
        VenueId = venueId;
        Name = name;
        Type = type;
        Surface = surface;
        Covered = covered;
        SlotDurationMinutes = slotDurationMinutes;
        Active = active;
    }
}
