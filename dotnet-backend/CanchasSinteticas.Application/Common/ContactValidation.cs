using System.Text.RegularExpressions;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Application.Common;

/// <summary>
/// Validaciones de datos de contacto (teléfono) compartidas por los casos de uso.
/// Refleja, como autoridad final, la misma política aplicada en el cliente.
/// </summary>
public static class ContactValidation
{
    /// <summary>
    /// Valida un teléfono colombiano opcional. Null o vacío es válido. Si viene un valor:
    /// admite `+57` opcional, debe quedar en 10 dígitos (celular 3XX o fijo 60X) y no puede
    /// ser una secuencia de un mismo dígito repetido (número basura, ej. 32222222222).
    /// Lanza <see cref="ValidationError"/> si no cumple.
    /// </summary>
    public static void EnsurePhoneValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;

        var trimmed = phone.Trim();
        if (!Regex.IsMatch(trimmed, @"^[+\d\s()-]+$"))
            throw new ValidationError("El teléfono no es válido.");

        var digits = Regex.Replace(trimmed, @"\D", string.Empty);
        if (digits.Length == 12 && digits.StartsWith("57", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.Length != 10)
            throw new ValidationError("El teléfono debe tener 10 dígitos (ej. 300 123 4567).");
        if (digits[0] != '3' && digits[0] != '6')
            throw new ValidationError("El teléfono debe ser un celular (3XX) o un fijo (60X).");
        if (Regex.IsMatch(digits, @"(\d)\1{6,}"))
            throw new ValidationError("Ingresa un número de teléfono real.");
    }

    /// <summary>Normaliza un teléfono opcional (recorta); null o vacío devuelve null.</summary>
    public static string? Normalize(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
