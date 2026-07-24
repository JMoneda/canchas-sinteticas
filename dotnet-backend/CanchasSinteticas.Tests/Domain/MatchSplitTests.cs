using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Tests.Domain;

/// <summary>Pruebas del pago dividido en el dominio (US2, T037): partes exactas y recaudo.</summary>
public class MatchSplitTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private static Match NewMatch(int maxPlayers, decimal total) =>
        new("m1", "res1", "org", maxPlayers, true, total, null, Now.AddDays(1), Now);

    [Fact]
    public void Rechaza_un_cupo_menor_a_dos()
    {
        Assert.Throws<ValidationError>(() => NewMatch(1, 80000m));
    }

    [Fact]
    public void Rechaza_un_cupo_por_encima_del_maximo_permitido()
    {
        Assert.Throws<ValidationError>(() => NewMatch(Match.MaxAllowedPlayers + 1, 80000m));
    }

    [Fact]
    public void La_suma_de_las_partes_iguala_exactamente_el_total()
    {
        // 100001 entre 4 → 25001, 25000, 25000, 25000 (el residuo se reparte de a uno).
        var match = NewMatch(4, 100001m);

        var sum = 0m;
        for (var i = 0; i < 4; i++)
        {
            sum += match.ShareFor(i);
        }

        Assert.Equal(100001m, sum);
        Assert.Equal(25001m, match.ShareFor(0));
        Assert.Equal(25000m, match.ShareFor(3));
    }

    [Fact]
    public void AmountCollected_suma_solo_las_partes_pagadas()
    {
        var match = NewMatch(4, 100000m);
        match.Join("org", "Org", Now);
        match.Join("p2", "P2", Now);

        Assert.Equal(0m, match.AmountCollected);

        match.ConfirmSharePayment("org", "pay-org");
        Assert.Equal(25000m, match.AmountCollected);
        Assert.False(match.IsFullyCollected);
    }

    [Fact]
    public void Se_cubre_el_total_cuando_todas_las_partes_pagan()
    {
        var match = NewMatch(2, 80000m);
        match.Join("a", "A", Now);
        match.Join("b", "B", Now);

        match.ConfirmSharePayment("a", "pa");
        match.ConfirmSharePayment("b", "pb");

        Assert.True(match.IsFullyCollected);
        Assert.Equal(80000m, match.AmountCollected);
    }

    [Fact]
    public void Al_salir_un_jugador_pagado_baja_el_recaudo()
    {
        var match = NewMatch(3, 90000m);
        match.Join("org", "Org", Now);
        match.Join("b", "B", Now);
        match.ConfirmSharePayment("b", "pb");
        Assert.Equal(30000m, match.AmountCollected);

        match.Leave("b");

        Assert.Equal(0m, match.AmountCollected);
    }
}
