using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.Services;

/// <summary>
/// Calcula el precio de una franja recorriéndola en sub-bloques de 30 minutos y
/// aplicando la regla de precio vigente en cada uno. Así soporta tarifas mixtas
/// (por ejemplo, una reserva que cruza de horario diurno a nocturno).
/// </summary>
public static class PricingCalculator
{
    private const int StepMinutes = 30;

    /// <summary>
    /// Calcula el precio total de la franja indicada según las reglas de precio.
    /// </summary>
    /// <exception cref="NoPriceConfiguredError">
    /// Si algún sub-bloque no tiene una tarifa configurada.
    /// </exception>
    public static decimal Calculate(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        IReadOnlyList<PriceRule> rules)
    {
        if (rules.Count == 0)
            throw new NoPriceConfiguredError();

        decimal total = 0m;
        var cursor = startTime;

        while (cursor < endTime)
        {
            var rule = rules.FirstOrDefault(r => r.AppliesTo(date, cursor))
                ?? throw new NoPriceConfiguredError();

            total += rule.PricePerHour * StepMinutes / 60m;
            cursor = cursor.AddMinutes(StepMinutes);
        }

        return total;
    }

    /// <summary>
    /// Devuelve el precio "desde" (más bajo) entre las reglas de una cancha, o null si no hay reglas.
    /// </summary>
    public static decimal? MinPricePerHour(IReadOnlyList<PriceRule> rules) =>
        rules.Count == 0 ? null : rules.Min(r => r.PricePerHour);
}
