using SunnyRides.Model.Enums;
using SunnyRides.Services.Placanja;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Kad klijent dobija poruku o povratu. Primjer iz reviewa: Stripe vrati povrat sa
/// statusom "failed", a klijent ipak dobije "Povrat sredstava je izvrsen".
/// </summary>
public class PovratTrebaJavitiTests
{
    [Theory]
    [InlineData(StatusPlacanja.Pending)]
    [InlineData(StatusPlacanja.Succeeded)]
    public void Prihvacen_povrat_se_javlja(StatusPlacanja status)
    {
        Assert.True(IznosiStripe.PovratTrebaJaviti(status));
    }

    [Theory]
    [InlineData(StatusPlacanja.Failed)]
    [InlineData(StatusPlacanja.Canceled)]
    [InlineData(StatusPlacanja.Created)]
    public void Odbijen_ponisten_ili_neposlan_povrat_se_ne_javlja(StatusPlacanja status)
    {
        Assert.False(IznosiStripe.PovratTrebaJaviti(status));
    }

    [Theory]
    [InlineData("failed", false)]
    [InlineData("canceled", false)]
    [InlineData("pending", true)]
    [InlineData("succeeded", true)]
    public void Odgovor_Stripea_odredjuje_da_li_se_javlja(string stripeStatus, bool javiti)
    {
        Assert.Equal(javiti, IznosiStripe.PovratTrebaJaviti(IznosiStripe.StatusPovrata(stripeStatus)));
    }
}
