using System.Security.Cryptography;
using System.Text;

namespace CanchasSinteticas.Infrastructure.Payments;

/// <summary>
/// Verifica la autenticidad de los eventos de webhook de Wompi. Wompi calcula el checksum como
/// SHA-256 de la concatenación de los valores de las propiedades firmadas (en orden), seguido del
/// timestamp del evento y del <c>events secret</c>.
/// </summary>
public class WompiSignatureVerifier
{
    /// <summary>Calcula el checksum esperado para los valores, timestamp y secreto indicados.</summary>
    public string ComputeChecksum(IEnumerable<string> propertyValues, string timestamp, string eventsSecret)
    {
        var raw = string.Concat(propertyValues) + timestamp + eventsSecret;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Indica si el checksum recibido coincide con el calculado (comparación insensible a mayúsculas).</summary>
    public bool Verify(IEnumerable<string> propertyValues, string timestamp, string eventsSecret, string? providedChecksum)
    {
        if (string.IsNullOrWhiteSpace(providedChecksum))
            return false;

        var expected = ComputeChecksum(propertyValues, timestamp, eventsSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(providedChecksum.ToUpperInvariant()));
    }
}
