using SunnyRides.Services.Dostupnost;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Buffer se primjenjuje na jednom mjestu i ovi testovi to mjesto pribijaju.
///
/// Sam uslov preklapanja se ne testira ovdje nego kroz API, nad seed podacima -
/// napisati ga drugi put kao C# poredjenje znacilo bi dvije implementacije istog
/// pravila, a onda test dokazuje samo da se te dvije slazu.
/// </summary>
public class UslovDostupnostiTests
{
    private static readonly DateTime Termin = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BufferJeDvaSata()
    {
        Assert.Equal(2, UslovDostupnosti.Buffer.TotalHours);
    }

    [Fact]
    public void DonjaGranicaSePomjeraUnazad()
    {
        Assert.Equal(Termin.AddHours(-2), UslovDostupnosti.GranicaOd(Termin));
    }

    [Fact]
    public void GornjaGranicaSePomjeraUnaprijed()
    {
        Assert.Equal(Termin.AddHours(2), UslovDostupnosti.GranicaDo(Termin));
    }

    [Fact]
    public void ProsireniPeriodJeCetiriSataDuziOdTrazenog()
    {
        var trazenoOd = Termin;
        var trazenoDo = Termin.AddDays(3);

        var prosireno = UslovDostupnosti.GranicaDo(trazenoDo) - UslovDostupnosti.GranicaOd(trazenoOd);

        Assert.Equal((trazenoDo - trazenoOd).TotalHours + 4, prosireno.TotalHours);
    }
}
