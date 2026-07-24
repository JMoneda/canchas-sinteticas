namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Modelo de recaudo de una sede: define a dónde llega el dinero de los pagos.
/// </summary>
public enum SettlementMode
{
    /// <summary>La plataforma recauda en su cuenta central y liquida al dueño.</summary>
    Marketplace,

    /// <summary>El dueño recauda directamente con su propia cuenta del proveedor.</summary>
    Direct,
}
