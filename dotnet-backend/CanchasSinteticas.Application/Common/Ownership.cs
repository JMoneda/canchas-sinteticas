using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Common;

/// <summary>
/// Comprobaciones de propiedad multi-tenant: garantizan que un dueño solo opere
/// sobre sus propias sedes y canchas.
/// </summary>
public static class Ownership
{
    /// <summary>Obtiene una sede verificando que pertenezca al dueño indicado.</summary>
    public static Venue OwnedVenue(IVenueRepository venues, string ownerId, string venueId)
    {
        var venue = venues.GetById(venueId) ?? throw new VenueNotFoundError();
        if (venue.OwnerId != ownerId)
            throw new NotAuthorizedError();
        return venue;
    }

    /// <summary>Obtiene una cancha verificando que pertenezca a una sede del dueño indicado.</summary>
    public static Court OwnedCourt(
        IVenueRepository venues,
        ICourtRepository courts,
        string ownerId,
        string courtId)
    {
        var court = courts.GetById(courtId) ?? throw new CourtNotFoundError();
        OwnedVenue(venues, ownerId, court.VenueId);
        return court;
    }
}
