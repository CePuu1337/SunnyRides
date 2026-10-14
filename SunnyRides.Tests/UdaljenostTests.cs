using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Lokacija;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>Zracna udaljenost i provjera para koordinata.</summary>
public class UdaljenostTests
{
    [Fact]
    public void Ista_tacka_je_na_nuli()
    {
        Assert.Equal(0d, Udaljenost.Km(43.3438, 17.8078, 43.3438, 17.8078), precision: 6);
    }

    [Fact]
    public void Mostar_Sarajevo_je_oko_75_km_zracno()
    {
        var km = Udaljenost.Km(43.3438, 17.8078, 43.8563, 18.4131);

        Assert.InRange(km, 74, 76);
    }

    [Fact]
    public void Smjer_mjerenja_nije_bitan()
    {
        var tamo = Udaljenost.Km(42.2911, 18.8403, 42.6507, 18.0944);
        var nazad = Udaljenost.Km(42.6507, 18.0944, 42.2911, 18.8403);

        Assert.Equal(tamo, nazad, precision: 9);
    }

    [Fact]
    public void Suprotne_tacke_na_ekvatoru_su_pola_obima()
    {
        // Kod suprotnih tacaka zaokruzivanje moze dati vrijednost malo iznad 1, a Asin
        // bi tada vratio NaN.
        var km = Udaljenost.Km(0, 0, 0, 180);

        Assert.False(double.IsNaN(km));
        Assert.InRange(km, 20_000, 20_030);
    }

    [Fact]
    public void Za_Sarajevo_je_Mostar_najbliza_poslovnica()
    {
        // Sarajevo nema poslovnicu. Od cetiri poslovnice iz seeda najbliza mu je Mostar.
        var poslovnice = new Dictionary<string, (double Sirina, double Duzina)>
        {
            ["Neum"] = (42.9230, 17.6120),
            ["Mostar"] = (43.3438, 17.8078),
            ["Split"] = (43.5081, 16.4402),
            ["Dubrovnik"] = (42.6507, 18.0944)
        };

        var najbliza = poslovnice
            .OrderBy(x => Udaljenost.Km(43.8563, 18.4131, x.Value.Sirina, x.Value.Duzina))
            .First().Key;

        Assert.Equal("Mostar", najbliza);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(43.0, 17.0)]
    [InlineData(-90.0, 180.0)]
    public void Ispravan_par_prolazi(double? latituda, double? longituda)
    {
        Koordinate.ProvjeriPar(latituda, longituda);
    }

    [Theory]
    [InlineData(43.0, null)]
    [InlineData(null, 17.0)]
    [InlineData(91.0, 17.0)]
    [InlineData(43.0, -181.0)]
    public void Nepotpun_ili_nemoguc_par_se_odbija(double? latituda, double? longituda)
    {
        Assert.Throws<BusinessException>(() => Koordinate.ProvjeriPar(latituda, longituda));
    }
}
