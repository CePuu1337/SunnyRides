using SunnyRides.Services.Preporuke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Obrazlozenje uz preporuku iz modela: kratko kaze iz cega je model za korisnika
/// ucio, i ne navodi nesto cega u njegovoj historiji nema.
/// </summary>
public class ObrazlozenjePredikcijeTests
{
    [Fact]
    public void Navodi_najmove_i_ocjene_kad_ih_korisnik_ima()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            new RecommenderService.HistorijaKorisnika(4, 3));

        Assert.Equal("Odabrano prema vašim najmovima i ocjenama, uz ocjene drugih korisnika.", tekst);
    }

    [Fact]
    public void Bez_ocjena_ne_spominje_vlastite_ocjene()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            new RecommenderService.HistorijaKorisnika(2, 0));

        Assert.Equal("Odabrano prema vašim najmovima, uz ocjene drugih korisnika.", tekst);
    }

    [Fact]
    public void Bez_najmova_ne_spominje_vlastite_najmove()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            new RecommenderService.HistorijaKorisnika(0, 2));

        Assert.Equal("Odabrano prema vašim ocjenama, uz ocjene drugih korisnika.", tekst);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bez_vlastite_historije_navodi_slicne_korisnike(bool prazanZapis)
    {
        var historija = prazanZapis ? new RecommenderService.HistorijaKorisnika(0, 0) : null;

        var tekst = RecommenderService.ObrazloziPredikciju(historija);

        Assert.Equal("Odabrano prema ocjenama korisnika sličnog ukusa.", tekst);
    }

    [Fact]
    public void Tekst_je_kratak_za_karticu()
    {
        var tekst = RecommenderService.ObrazloziPredikciju(
            new RecommenderService.HistorijaKorisnika(62, 54));

        Assert.True(tekst.Length <= 80, $"Obrazlozenje ima {tekst.Length} znakova.");
    }
}
