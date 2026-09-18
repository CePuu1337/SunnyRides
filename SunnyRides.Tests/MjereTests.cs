using SunnyRides.Services.Preporuke.Ml;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Mjere greske modela. Najvise je vrijedno da se zna kako se cita R kvadrat, jer je to
/// broj koji na malo podataka zna ispasti negativan i onda izgleda kao kvar.
/// </summary>
public class MjereTests
{
    private static RezultatMjerenja Izracunaj(params (double Stvarno, double Predvidjeno)[] parovi) =>
        Mjere.Izracunaj(parovi);

    [Fact]
    public void SavrsenaPredikcijaNemaGresku()
    {
        var r = Izracunaj((5, 5), (4, 4), (2, 2));

        Assert.Equal(0, r.Rmse, 6);
        Assert.Equal(0, r.Mae, 6);
        Assert.Equal(1, r.RKvadrat, 6);
    }

    [Fact]
    public void RmseKaznjavaVelikuGreskuViseNegoMae()
    {
        var r = Izracunaj((5, 5), (5, 5), (5, 1));

        Assert.Equal(4.0 / 3, r.Mae, 6);
        Assert.Equal(Math.Sqrt(16.0 / 3), r.Rmse, 6);
        Assert.True(r.Rmse > r.Mae);
    }

    /// <summary>Model koji uvijek pogodi prosjek radi tacno kao i najprostije predvidjanje.</summary>
    [Fact]
    public void PogadjanjeProsjekaDajeNulaRKvadrat()
    {
        var r = Izracunaj((2, 4), (4, 4), (6, 4));

        Assert.Equal(0, r.RKvadrat, 6);
    }

    [Fact]
    public void LosijiOdProsjekaDajeNegativanRKvadrat()
    {
        var r = Izracunaj((2, 6), (4, 4), (6, 2));

        Assert.True(r.RKvadrat < 0);
    }

    /// <summary>Kad su sve ocjene iste, nema odstupanja koje bi se objasnilo, pa ni poredjenja.</summary>
    [Fact]
    public void IsteOcjeneDajuNulaUmjestoDijeljenjaNulom()
    {
        var r = Izracunaj((4, 3), (4, 5), (4, 4));

        Assert.Equal(0, r.RKvadrat, 6);
        Assert.Equal(3, r.BrojMjerenja);
    }

    /// <summary>
    /// Osnovna greska je ono sto bi imalo pogadjanje prosjeka. Uz nju se RMSE moze
    /// citati - sam po sebi ne govori je li model dobar.
    /// </summary>
    [Fact]
    public void OsnovnaGreskaJeGreskaPogadjanjaProsjeka()
    {
        var r = Izracunaj((2, 2), (4, 4), (6, 6));

        // prosjek je 4, odstupanja su -2, 0, 2
        Assert.Equal(Math.Sqrt(8.0 / 3), r.RmseOsnovni, 6);
        Assert.Equal(0, r.Rmse, 6);
    }

    [Fact]
    public void PrazanSkupNePuca()
    {
        var r = Mjere.Izracunaj(Array.Empty<(double, double)>());

        Assert.Equal(0, r.BrojMjerenja);
    }
}
