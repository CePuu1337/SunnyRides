using SunnyRides.Services.Dozvole;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Model vozila mora stati u pravilo kategorije koja mu je upisana. Inace bi ga
/// pretraga nudila klijentima cija ga dozvola ne pokriva.
/// </summary>
public class UskladjenostModelaTests
{
    private static readonly PraviloUlaz A1Skuter = new(1, 1, MaxKubikaza: 125, MaxSnagaKw: 11m, MinGodine: 16);
    private static readonly PraviloUlaz AMotocikl = new(2, 2, MaxKubikaza: null, MaxSnagaKw: null, MinGodine: 24);

    [Fact]
    public void Skuter_od_125_ccm_odgovara_kategoriji_A1()
    {
        Assert.Null(UskladjenostModela.Razlog("A1", "Skuter", 125, 9m, A1Skuter));
    }

    [Fact]
    public void Model_preko_granice_kubikaze_se_odbija()
    {
        var razlog = UskladjenostModela.Razlog("A1", "Skuter", 649, 9m, A1Skuter);

        Assert.NotNull(razlog);
        Assert.Contains("125 ccm", razlog);
        Assert.Contains("649 ccm", razlog);
    }

    [Fact]
    public void Model_preko_granice_snage_se_odbija()
    {
        var razlog = UskladjenostModela.Razlog("A1", "Skuter", 125, 15m, A1Skuter);

        Assert.NotNull(razlog);
        Assert.Contains("11 kW", razlog);
    }

    [Fact]
    public void Kategorija_bez_granica_pokriva_svaki_model_tog_tipa()
    {
        Assert.Null(UskladjenostModela.Razlog("A", "Motocikl", 1200, 120m, AMotocikl));
    }

    [Fact]
    public void Kategorija_bez_pravila_za_tip_se_odbija()
    {
        Assert.NotNull(UskladjenostModela.Razlog("B", "Skuter", 125, 9m, pravilo: null));
    }

    [Fact]
    public void Elektricno_vozilo_bez_kubikaze_se_mjeri_snagom()
    {
        Assert.Null(UskladjenostModela.Razlog("A1", "Skuter", 0, 3.5m, A1Skuter));
    }
}
