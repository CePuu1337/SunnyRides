using SunnyRides.Services.Cijene;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Cijena koju klijent vidi uz vozilo i po kojoj se filtrira pretraga. Primjer iz
/// reviewa: vozilo od 40 EUR u sezoni sa mnoziocem 1,3 kosta 52 EUR, pa ne smije
/// proci filter "do 45 EUR".
/// </summary>
public class EfektivnaTarifaTests
{
    [Fact]
    public void Sezonski_mnozilac_se_primjenjuje_na_tarifu_vozila()
    {
        Assert.Equal(52m, PricingService.EfektivnaTarifa(40m, tarifaSezone: null, mnozilac: 1.3m));
    }

    [Fact]
    public void Tarifa_iz_cjenovnika_zamjenjuje_tarifu_vozila()
    {
        Assert.Equal(55m, PricingService.EfektivnaTarifa(40m, tarifaSezone: 50m, mnozilac: 1.1m));
    }

    [Fact]
    public void Bez_sezone_vazi_tarifa_vozila()
    {
        Assert.Equal(40m, PricingService.EfektivnaTarifa(40m, tarifaSezone: null, mnozilac: null));
    }

    [Fact]
    public void Zaokruzuje_se_na_dvije_decimale_kao_u_obracunu()
    {
        // 25,55 x 1,15 = 29,3825
        Assert.Equal(29.38m, PricingService.EfektivnaTarifa(25.55m, tarifaSezone: null, mnozilac: 1.15m));
    }
}
