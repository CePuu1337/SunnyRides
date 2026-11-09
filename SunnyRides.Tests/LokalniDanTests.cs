using SunnyRides.Services.Vrijeme;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Granice dana u izvjestajima i pregledu poslovanja. Desktop salje lokalnu ponoc
/// pretvorenu u UTC: 1. oktobar stize kao "30.09. 22:00Z". Prije ispravke je server
/// odsjekao sate i dobio 30. septembar, pa je izvjestaj za oktobar pocinjao dan ranije.
/// </summary>
public class LokalniDanTests
{
    [Fact]
    public void Lokalna_ponoc_poslana_kao_utc_je_taj_lokalni_dan()
    {
        var poslano = new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 1), LokalnoVrijeme.LokalniDatum(poslano));
    }

    [Fact]
    public void Datum_bez_zone_je_vec_lokalni_dan()
    {
        var poslano = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(new DateTime(2026, 10, 1), LokalnoVrijeme.LokalniDatum(poslano));
    }

    [Fact]
    public void Ljetna_lokalna_ponoc_je_dva_sata_ranije_u_utc()
    {
        Assert.Equal(
            new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc),
            LokalnoVrijeme.UUtc(new DateTime(2026, 10, 1)));
    }

    [Fact]
    public void Zimska_lokalna_ponoc_je_sat_ranije_u_utc()
    {
        Assert.Equal(
            new DateTime(2026, 10, 31, 23, 0, 0, DateTimeKind.Utc),
            LokalnoVrijeme.UUtc(new DateTime(2026, 11, 1)));
    }

    [Fact]
    public void Dan_promjene_sata_ima_dvadeset_pet_sati()
    {
        var pocetak = LokalnoVrijeme.UUtc(new DateTime(2026, 10, 25));
        var kraj = LokalnoVrijeme.UUtc(new DateTime(2026, 10, 26));

        Assert.Equal(TimeSpan.FromHours(25), kraj - pocetak);
    }
}
