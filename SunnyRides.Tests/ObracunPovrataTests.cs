using SunnyRides.Services.Primopredaje;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Obracun depozita pri povratu. Najvise paznje traze granice tolerancije - 59 i 60
/// minuta kasnjenja - jer tu klijent ili ne placa nista ili placa cijeli dan.
/// </summary>
public class ObracunPovrataTests
{
    private static readonly DateTime Ugovoreno = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);

    private static RezultatPovrata Obracunaj(
        double kasnjenjeMinuta, decimal dnevna = 40m, decimal depozit = 200m, decimal steta = 0m) =>
        ObracunPovrata.Izracunaj(new UlazPovrata(
            Ugovoreno, Ugovoreno.AddMinutes(kasnjenjeMinuta), dnevna, depozit, steta));

    [Theory]
    [InlineData(-120, 0)]        // vraceno ranije
    [InlineData(0, 0)]
    [InlineData(30, 0)]
    [InlineData(59, 0)]          // jos u toleranciji
    [InlineData(60, 1)]          // prvi minut preko tolerancije je cijeli dan
    [InlineData(23 * 60, 1)]
    [InlineData(24 * 60, 1)]
    [InlineData(24 * 60 + 30, 1)]  // dan i pola sata - ostatak je u toleranciji
    [InlineData(24 * 60 + 59, 1)]
    [InlineData(25 * 60, 2)]
    [InlineData(49 * 60, 3)]
    public void DaniPrekoracenja(double minuta, int ocekivano)
    {
        Assert.Equal(ocekivano, Obracunaj(minuta).DanaPrekoracenja);
    }

    [Fact]
    public void NaVrijemeVracaCijeliDepozit()
    {
        var r = Obracunaj(0);

        Assert.True(r.UnutarTolerancije);
        Assert.Equal(0m, r.Doplata);
        Assert.Equal(200m, r.PovratDepozita);
        Assert.Equal(0m, r.ZadrzanoOdDepozita);
    }

    [Fact]
    public void KasnjenjeUToleranciji_NeNaplacujeSeAliSeBiljezi()
    {
        var r = Obracunaj(45);

        Assert.True(r.UnutarTolerancije);
        Assert.Equal(45, r.KasnjenjeMinuta);
        Assert.Equal(200m, r.PovratDepozita);
    }

    [Fact]
    public void StetaIDoplataSeOdbijajuOdDepozita()
    {
        // Dva dana kasnjenja po 40 = 80, steta 50, ukupno 130 od 200.
        var r = Obracunaj(25 * 60, steta: 50m);

        Assert.Equal(80m, r.Doplata);
        Assert.Equal(130m, r.ZadrzanoOdDepozita);
        Assert.Equal(70m, r.PovratDepozita);
        Assert.Equal(0m, r.NepokrivenoDepozitom);
    }

    [Fact]
    public void StetaVecaOdDepozita_DepozitSeZadrzavaCijeliAOstatakJeNepokriven()
    {
        var r = Obracunaj(0, steta: 350m);

        Assert.Equal(200m, r.ZadrzanoOdDepozita);
        Assert.Equal(0m, r.PovratDepozita);
        Assert.Equal(150m, r.NepokrivenoDepozitom);
    }

    [Fact]
    public void ZadrzanoIPovratUvijekDajuDepozit()
    {
        foreach (var steta in new[] { 0m, 10m, 199.99m, 200m, 500m })
        {
            var r = Obracunaj(90, dnevna: 33.33m, steta: steta);

            Assert.Equal(200m, r.ZadrzanoOdDepozita + r.PovratDepozita);
            Assert.Equal(r.Doplata + r.IznosStete, r.ZadrzanoOdDepozita + r.NepokrivenoDepozitom);
        }
    }

    [Fact]
    public void BezDepozitaNemaPovrata()
    {
        var r = Obracunaj(0, depozit: 0m, steta: 20m);

        Assert.Equal(0m, r.PovratDepozita);
        Assert.Equal(20m, r.NepokrivenoDepozitom);
    }
}
