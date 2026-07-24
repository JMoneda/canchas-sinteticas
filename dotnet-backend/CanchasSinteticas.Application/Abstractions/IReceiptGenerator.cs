using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Application.Abstractions;

/// <summary>Genera la representación descargable (PDF) de un comprobante.</summary>
public interface IReceiptGenerator
{
    /// <summary>Devuelve el PDF del comprobante como bytes.</summary>
    byte[] GeneratePdf(Receipt receipt);
}
