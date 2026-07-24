using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Configura el modelo de recaudo de una sede (marketplace o cuenta directa del dueño), respetando la
/// propiedad multi-tenant.
/// </summary>
public class VenuePaymentConfigService(IVenueRepository venues)
{
    /// <summary>Define el modelo de recaudo de una sede del dueño.</summary>
    public VenuePaymentConfigOutput Set(string ownerId, string venueId, VenuePaymentConfigInput input)
    {
        var venue = Ownership.OwnedVenue(venues, ownerId, venueId);

        if (!Enum.TryParse<SettlementMode>(input.SettlementMode, ignoreCase: true, out var mode))
            throw new ValidationError($"Modelo de recaudo inválido: '{input.SettlementMode}'.");

        var merchantRef = string.IsNullOrWhiteSpace(input.GatewayMerchantRef) ? null : input.GatewayMerchantRef.Trim();
        if (mode == SettlementMode.Direct && merchantRef is null)
            throw new ValidationError("El modo de cuenta directa requiere el identificador de comercio del dueño.");

        venue.SettlementMode = mode;
        venue.GatewayMerchantRef = mode == SettlementMode.Direct ? merchantRef : null;
        venues.Update(venue);

        return new VenuePaymentConfigOutput(venue.Id, mode.ToString().ToLowerInvariant(), venue.GatewayMerchantRef);
    }

    /// <summary>Obtiene la configuración de recaudo actual de una sede del dueño.</summary>
    public VenuePaymentConfigOutput Get(string ownerId, string venueId)
    {
        var venue = Ownership.OwnedVenue(venues, ownerId, venueId);
        return new VenuePaymentConfigOutput(venue.Id, venue.SettlementMode.ToString().ToLowerInvariant(), venue.GatewayMerchantRef);
    }
}
