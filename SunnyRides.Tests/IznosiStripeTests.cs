using SunnyRides.Model.Enums;
using SunnyRides.Services.Placanja;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Pretvaranje iznosa i statusa izmedju Stripe-a i baze. Potvrda placanja poredi
/// naplaceno sa ocekivanim u centima, pa greska od jednog centa ovdje znaci da se
/// nijedna rezervacija ne potvrdi.
/// </summary>
public class IznosiStripeTests
{
    [Theory]
    [InlineData("250.30", 25030)]
    [InlineData("0.01", 1)]
    [InlineData("99.995", 10000)]   // pola centa se zaokruzuje navise, ne na parno
    [InlineData("99.994", 9999)]
    [InlineData("1234.5", 123450)]
    [InlineData("0", 0)]
    public void EuriUCente(string eura, long centi)
    {
        Assert.Equal(centi, IznosiStripe.UCente(decimal.Parse(eura, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void CentiNazadUEureBezGubitka()
    {
        const decimal iznos = 318.79m;

        Assert.Equal(iznos, IznosiStripe.IzCenti(IznosiStripe.UCente(iznos)));
    }

    [Theory]
    [InlineData("succeeded", StatusPlacanja.Succeeded)]
    [InlineData("canceled", StatusPlacanja.Canceled)]
    [InlineData("processing", StatusPlacanja.Pending)]
    [InlineData("requires_action", StatusPlacanja.Pending)]
    [InlineData("requires_capture", StatusPlacanja.Pending)]
    [InlineData("requires_payment_method", StatusPlacanja.Created)]  // odbijena kartica - moze se pokusati ponovo
    [InlineData("requires_confirmation", StatusPlacanja.Created)]
    [InlineData(null, StatusPlacanja.Created)]
    public void StatusIntenta(string? stripe, StatusPlacanja ocekivano)
    {
        Assert.Equal(ocekivano, IznosiStripe.StatusIntenta(stripe));
    }

    [Theory]
    [InlineData("succeeded", StatusPlacanja.Succeeded)]
    [InlineData("failed", StatusPlacanja.Failed)]
    [InlineData("canceled", StatusPlacanja.Canceled)]
    [InlineData("pending", StatusPlacanja.Pending)]
    [InlineData("requires_action", StatusPlacanja.Pending)]
    public void StatusPovrata(string stripe, StatusPlacanja ocekivano)
    {
        Assert.Equal(ocekivano, IznosiStripe.StatusPovrata(stripe));
    }

    [Theory]
    [InlineData(StatusPlacanja.Created, true)]
    [InlineData(StatusPlacanja.Pending, true)]
    [InlineData(StatusPlacanja.Succeeded, false)]
    [InlineData(StatusPlacanja.Failed, false)]
    [InlineData(StatusPlacanja.Canceled, false)]
    public void OtvorenoPlacanje(StatusPlacanja status, bool otvoreno)
    {
        Assert.Equal(otvoreno, IznosiStripe.JeOtvoreno(status));
    }

    [Theory]
    [InlineData(StatusPlacanja.Created, true)]     // odobren, jos nije potvrdjen - racuna se, da se ne posalje dvaput
    [InlineData(StatusPlacanja.Pending, true)]
    [InlineData(StatusPlacanja.Succeeded, true)]
    [InlineData(StatusPlacanja.Failed, false)]     // novac nije vracen
    [InlineData(StatusPlacanja.Canceled, false)]
    public void VazeciPovrat(StatusPlacanja status, bool vazeci)
    {
        Assert.Equal(vazeci, IznosiStripe.PovratJeVazeci(status));
    }
}
