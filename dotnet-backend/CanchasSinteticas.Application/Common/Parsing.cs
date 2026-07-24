using System.Globalization;
using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Application.Common;

/// <summary>
/// Utilidades de parseo de valores de entrada a tipos de dominio, con validación.
/// </summary>
public static class Parsing
{
    /// <summary>Parsea una fecha en formato yyyy-MM-dd.</summary>
    public static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new ValidationError($"Fecha inválida: '{value}'. Use el formato yyyy-MM-dd.");

    /// <summary>Parsea una fecha opcional; vacío o null devuelve null.</summary>
    public static DateOnly? ParseDateOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDate(value);

    /// <summary>Parsea una hora en formato HH:mm.</summary>
    public static TimeOnly ParseTime(string value) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : throw new ValidationError($"Hora inválida: '{value}'. Use el formato HH:mm.");

    /// <summary>Parsea el tipo de cancha.</summary>
    public static CourtType ParseCourtType(string value) =>
        Enum.TryParse<CourtType>(value, true, out var type)
            ? type
            : throw new ValidationError($"Tipo de cancha inválido: '{value}'.");

    /// <summary>Parsea el día de la semana; vacío o null significa cualquier día.</summary>
    public static DayOfWeek? ParseDayOfWeek(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<DayOfWeek>(value, true, out var day)
            ? day
            : throw new ValidationError($"Día de la semana inválido: '{value}'.");
    }

    /// <summary>Parsea el rol permitido en el registro (Owner o Client).</summary>
    public static UserRole ParseRegistrationRole(string value)
    {
        if (!Enum.TryParse<UserRole>(value, true, out var role))
            throw new ValidationError($"Rol inválido: '{value}'.");

        if (role == UserRole.SuperAdmin)
            throw new ValidationError("No es posible registrarse como administrador de la plataforma.");

        return role;
    }

    /// <summary>Parsea el medio de pago; por defecto pasarela en línea.</summary>
    public static PaymentMethod ParsePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PaymentMethod.OnlineGateway;

        return Enum.TryParse<PaymentMethod>(value, true, out var method)
            ? method
            : throw new ValidationError($"Medio de pago inválido: '{value}'.");
    }
}
