using SunnyRides.Services.Rezervacije;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Uslovi otkazivanja koje klijent vidi prije rezervacije moraju se slagati sa
/// obracunom povrata koji se radi kad stvarno otkaze.
/// </summary>
public class UsloviOtkazivanjaTests
{
    private static readonly DateTime Preuzimanje = new(2026, 12, 20, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Deset_dana_prije_postoje_sva_tri_perioda_sa_datumima()
    {
        var periodi = PravilaOtkazivanja.Uslovi(Preuzimanje, Preuzimanje.AddDays(-10));

        Assert.Equal(3, periodi.Count);
        Assert.Equal(100m, periodi[0].ProcenatPovrataNajma);
        Assert.Equal(Preuzimanje.AddDays(-7), periodi[0].Do);
        Assert.Equal(50m, periodi[1].ProcenatPovrataNajma);
        Assert.Equal(Preuzimanje.AddDays(-3), periodi[1].Do);
        Assert.Equal(0m, periodi[2].ProcenatPovrataNajma);
        Assert.Null(periodi[2].Do);
    }

    [Fact]
    public void Pet_dana_prije_puni_povrat_je_vec_prosao()
    {
        var periodi = PravilaOtkazivanja.Uslovi(Preuzimanje, Preuzimanje.AddDays(-5));

        Assert.Equal(new[] { 50m, 0m }, periodi.Select(x => x.ProcenatPovrataNajma));
        Assert.Null(periodi[0].Od);
    }

    [Fact]
    public void Dan_prije_ostaje_samo_bez_povrata()
    {
        var periodi = PravilaOtkazivanja.Uslovi(Preuzimanje, Preuzimanje.AddDays(-1));

        var period = Assert.Single(periodi);
        Assert.Equal(0m, period.ProcenatPovrataNajma);
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(-7.5)]
    [InlineData(-7)]
    [InlineData(-6.9)]
    [InlineData(-3)]
    [InlineData(-2.9)]
    [InlineData(-0.5)]
    public void Prvi_period_ima_isti_procenat_koji_obracun_daje_za_otkazivanje_sada(double danaOdPreuzimanja)
    {
        var sada = Preuzimanje.AddDays(danaOdPreuzimanja);

        var prikazano = PravilaOtkazivanja.Uslovi(Preuzimanje, sada)[0].ProcenatPovrataNajma;
        var obracunato = PravilaOtkazivanja.ProcenatPovrataNajma((Preuzimanje - sada).TotalDays, otkazujeAgencija: false);

        Assert.Equal(obracunato, prikazano);
    }
}
