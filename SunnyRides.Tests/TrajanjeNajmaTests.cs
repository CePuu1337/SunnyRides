using SunnyRides.Services.Cijene;
using SunnyRides.Services.Exceptions;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Kontrolni primjeri iz uputstva. Ovo su granicni slucajevi na kojima se obracun
/// najlakse lomi, pa su i jedini dio cijene koji ima vlastite testove po nalogu.
/// </summary>
public class TrajanjeNajmaTests
{
    private static readonly DateTime Pocetak = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(5, 0, 5)]      // pet sati
    [InlineData(1, 0, 1)]      // jedan sat
    [InlineData(6, 0, 6)]      // tacno sest sati je jos uvijek satna naplata
    public void NajamDoSestSati_NaplacujeSePoSatu(int sati, int minuta, int ocekivaniSati)
    {
        var trajanje = TrajanjeNajma.Izracunaj(
            Pocetak, Pocetak.AddHours(sati).AddMinutes(minuta));

        Assert.True(trajanje.PoSatu);
        Assert.Equal(ocekivaniSati, trajanje.Sati);
        Assert.Equal(0, trajanje.Dani);
    }

    [Fact]
    public void ZapocetiSatSeRacunaCijeli()
    {
        var trajanje = TrajanjeNajma.Izracunaj(Pocetak, Pocetak.AddHours(2).AddMinutes(1));

        Assert.True(trajanje.PoSatu);
        Assert.Equal(3, trajanje.Sati);
    }

    [Theory]
    [InlineData(6, 1)]         // minut preko satne granice vec je dnevna naplata
    [InlineData(12, 0)]
    [InlineData(24, 0)]        // tacno dvadeset cetiri sata je jedan dan
    public void NajamOdSestDoDvadesetCetiriSata_NaplacujeSeJedanDan(int sati, int minuta)
    {
        var trajanje = TrajanjeNajma.Izracunaj(
            Pocetak, Pocetak.AddHours(sati).AddMinutes(minuta));

        Assert.False(trajanje.PoSatu);
        Assert.Equal(1, trajanje.Dani);
    }

    [Theory]
    [InlineData(24, 30, 1)]    // kontrolni primjer: 24 h 30 min -> 1 dan
    [InlineData(24, 59, 1)]    // tacno na granici tolerancije, jos uvijek jedan dan
    [InlineData(25, 0, 2)]     // kontrolni primjer: 25 h -> 2 dana
    [InlineData(48, 0, 2)]     // kontrolni primjer: 48 h -> 2 dana
    [InlineData(49, 0, 3)]     // kontrolni primjer: 49 h -> 3 dana
    [InlineData(168, 0, 7)]    // sedam punih dana
    public void PrekoDvadesetCetiriSata_RacunaSeToleranciraOdPedesetDevetMinuta(
        int sati, int minuta, int ocekivaniDani)
    {
        var trajanje = TrajanjeNajma.Izracunaj(
            Pocetak, Pocetak.AddHours(sati).AddMinutes(minuta));

        Assert.False(trajanje.PoSatu);
        Assert.Equal(ocekivaniDani, trajanje.Dani);
    }

    [Fact]
    public void MinutPrekoTolerancije_DodajeCijeliDan()
    {
        var unutar = TrajanjeNajma.Izracunaj(Pocetak, Pocetak.AddHours(48).AddMinutes(59));
        var preko = TrajanjeNajma.Izracunaj(Pocetak, Pocetak.AddHours(49).AddMinutes(0));

        Assert.Equal(2, unutar.Dani);
        Assert.Equal(3, preko.Dani);
    }

    [Fact]
    public void NajamPoSatu_ZaOpremuSeRacunaKaoJedanDan()
    {
        var trajanje = TrajanjeNajma.Izracunaj(Pocetak, Pocetak.AddHours(5));

        Assert.Equal(0, trajanje.Dani);
        Assert.Equal(1, trajanje.DaniZaDodatke);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void DatumVracanjaPrijeIliJednakPreuzimanju_BacaGresku(int satiPomak)
    {
        Assert.Throws<BusinessException>(() =>
            TrajanjeNajma.Izracunaj(Pocetak, Pocetak.AddHours(satiPomak)));
    }
}
