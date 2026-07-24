using CanchasSinteticas.Domain.ValueObjects;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Sede o complejo deportivo. Pertenece a un dueño (Owner) y agrupa varias canchas.
/// Raíz del aislamiento multi-tenant: cada dueño solo gestiona sus propias sedes.
/// </summary>
public class Venue
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Identificador del usuario dueño de la sede.</summary>
    public string OwnerId { get; }

    /// <summary>Nombre comercial de la sede.</summary>
    public string Name { get; set; }

    /// <summary>Ciudad donde se ubica.</summary>
    public string City { get; set; }

    /// <summary>Dirección física.</summary>
    public string Address { get; set; }

    /// <summary>Ubicación geográfica (opcional, para mapa).</summary>
    public GeoLocation? Location { get; set; }

    /// <summary>Teléfono de contacto de la sede.</summary>
    public string? Phone { get; set; }

    /// <summary>URLs de fotos de la sede.</summary>
    public List<string> Photos { get; set; }

    /// <summary>Servicios disponibles (parqueo, camerinos, iluminación, cafetería...).</summary>
    public List<string> Services { get; set; }

    /// <summary>Hora de apertura.</summary>
    public TimeOnly OpeningTime { get; set; }

    /// <summary>Hora de cierre.</summary>
    public TimeOnly ClosingTime { get; set; }

    /// <summary>Horas de anticipación mínima para cancelar sin penalización.</summary>
    public int CancellationWindowHours { get; set; }

    /// <summary>Indica si la sede está activa y visible en el marketplace.</summary>
    public bool Active { get; set; }

    /// <summary>Fecha de alta.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Crea una sede.</summary>
    public Venue(
        string id,
        string ownerId,
        string name,
        string city,
        string address,
        GeoLocation? location,
        string? phone,
        List<string> photos,
        List<string> services,
        TimeOnly openingTime,
        TimeOnly closingTime,
        int cancellationWindowHours,
        bool active,
        DateTime createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        City = city;
        Address = address;
        Location = location;
        Phone = phone;
        Photos = photos;
        Services = services;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        CancellationWindowHours = cancellationWindowHours;
        Active = active;
        CreatedAt = createdAt;
    }
}
