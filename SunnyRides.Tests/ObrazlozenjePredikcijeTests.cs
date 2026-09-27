using SunnyRides.Services.Preporuke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Obrazlozenje uz preporuku iz modela: procjena se nikad ne prikazuje bez prosjeka
/// vozila, a uz nju stoji na cemu je model za korisnika ucio.
/// </summary>
public class ObrazlozenjePredikcijeTests
{
    [Fact]
    public void Procjena_stoji_uz_prosjek_vozila()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            4.8, 4.3, new RecommenderService.HistorijaKorisnika(4, 3));

        Assert.StartsWith("Za vas 4,8 od 5 (prosjek vozila 4,3).", tekst);
    }

    [Fact]
    public void Navodi_historiju_korisnika()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            4.8, 4.3, new RecommenderService.HistorijaKorisnika(4, 3));

        Assert.Contains("vasih 4 najma i 3 ocjene", tekst);
    }

    [Theory]
    [InlineData(1, "1 najma")]
    [InlineData(3, "3 najma")]
    [InlineData(5, "5 najmova")]
    [InlineData(11, "11 najmova")]
    [InlineData(21, "21 najma")]
    public void Broj_najmova_je_u_ispravnom_obliku(int najmova, string ocekivano)
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            4.0, 4.0, new RecommenderService.HistorijaKorisnika(najmova, 0));

        Assert.Contains(ocekivano, tekst);
    }

    [Fact]
    public void Vozilo_bez_recenzija_to_i_kaze()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(4.1, null, null);

        Assert.Contains("vozilo jos nema recenzija", tekst);
        Assert.DoesNotContain("prosjek vozila", tekst);
    }

    [Fact]
    public void Tekst_je_dovoljno_kratak_za_karticu()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            4.9, 4.1, new RecommenderService.HistorijaKorisnika(62, 54));

        Assert.True(tekst.Length <= 115, $"Obrazlozenje ima {tekst.Length} znakova.");
    }
}
