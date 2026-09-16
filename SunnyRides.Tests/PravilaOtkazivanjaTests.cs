using SunnyRides.Services.Rezervacije;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Povrat pri otkazivanju. Testira se pravilo, ne servis - bez baze i bez mokova, pa
/// test ne moze proci zato sto je mok podesen da vrati ocekivani rezultat.
///
/// Granice su ono sto je vazno: sedmi i treci dan su tacke na kojima se procenat
/// mijenja, a upravo tu se u praksi grijesi za jedan dan.
/// </summary>
public class PravilaOtkazivanjaTests
{
    private static readonly DateTime Sada = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Naplaceno 500, od toga 100 depozit - najam je 400.</summary>
    private static UlazOtkazivanja Ulaz(double danaDoPreuzimanja, bool agencija = false,
        decimal naplaceno = 500m, decimal depozit = 100m, decimal vecVraceno = 0m) =>
        new()
        {
            DatumOd = Sada.AddDays(danaDoPreuzimanja),
            Sada = Sada,
            OtkazujeAgencija = agencija,
            Naplaceno = naplaceno,
            IznosDepozita = depozit,
            VecVraceno = vecVraceno
        };

    [Theory]
    [InlineData(30, 100)]     // davno unaprijed
    [InlineData(8, 100)]      // iznad praga od sedam dana
    [InlineData(7.5, 100)]    // sedam i po dana je jos uvijek "vise od sedam"
    [InlineData(7, 50)]       // tacno sedam dana vise nije pun povrat
    [InlineData(5, 50)]
    [InlineData(3, 50)]       // tacno tri dana je jos uvijek polovina
    [InlineData(2.99, 0)]     // ispod tri dana nema povrata najma
    [InlineData(0.5, 0)]
    [InlineData(-1, 0)]       // termin je vec poceo
    public void ProcenatPratiPragove(double dana, int ocekivano)
    {
        var procenat = PravilaOtkazivanja.ProcenatPovrataNajma(dana, otkazujeAgencija: false);

        Assert.Equal(ocekivano, (int)procenat);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(1)]
    [InlineData(-5)]
    public void AgencijaUvijekVracaSve(double dana)
    {
        Assert.Equal(100m, PravilaOtkazivanja.ProcenatPovrataNajma(dana, otkazujeAgencija: true));
    }

    [Fact]
    public void DepozitSeVracaIKadNajamNe()
    {
        // Dan prije termina: najam propada u cijelosti, depozit se vraca u cijelosti.
        var obracun = PravilaOtkazivanja.Izracunaj(Ulaz(danaDoPreuzimanja: 1));

        Assert.Equal(400m, obracun.DioNajma);
        Assert.Equal(100m, obracun.DioDepozita);
        Assert.Equal(0m, obracun.PovratNajma);
        Assert.Equal(100m, obracun.PovratDepozita);
        Assert.Equal(100m, obracun.UkupanPovrat);
        Assert.Equal(400m, obracun.ZadrzanoAgenciji);
    }

    [Fact]
    public void PolovinaNajmaUzPuniDepozit()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(Ulaz(danaDoPreuzimanja: 5));

        Assert.Equal(50m, obracun.ProcenatPovrataNajma);
        Assert.Equal(200m, obracun.PovratNajma);
        Assert.Equal(300m, obracun.UkupanPovrat);
        Assert.Equal(200m, obracun.ZadrzanoAgenciji);
    }

    [Fact]
    public void RanoOtkazivanjeVracaSve()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(Ulaz(danaDoPreuzimanja: 20));

        Assert.Equal(500m, obracun.UkupanPovrat);
        Assert.Equal(0m, obracun.ZadrzanoAgenciji);
    }

    [Fact]
    public void OtkazivanjeAgencijeVracaSveIDanPrije()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(Ulaz(danaDoPreuzimanja: 1, agencija: true));

        Assert.Equal(500m, obracun.UkupanPovrat);
        Assert.Equal(0m, obracun.ZadrzanoAgenciji);
        Assert.Contains("Agencija", obracun.Obrazlozenje);
    }

    [Fact]
    public void NeplacenaRezervacijaNemaStaDaVrati()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(Ulaz(danaDoPreuzimanja: 10, naplaceno: 0m));

        Assert.Equal(0m, obracun.UkupanPovrat);
        Assert.Equal(0m, obracun.DioDepozita);
        Assert.Equal(0m, obracun.ZadrzanoAgenciji);
        Assert.Contains("nije placena", obracun.Obrazlozenje);
    }

    /// <summary>
    /// Bez ovoga bi dva uzastopna otkazivanja - ili ponovljen zahtjev poslije prekida
    /// veze - napravila dva povrata za isti novac.
    /// </summary>
    [Fact]
    public void VecVraceniIznosSeNePonavlja()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(
            Ulaz(danaDoPreuzimanja: 20, vecVraceno: 500m));

        Assert.Equal(0m, obracun.UkupanPovrat);

        // Pripadajuci povrat je i dalje pun - zadrzano ne raste time sto je vec placeno.
        Assert.Equal(0m, obracun.ZadrzanoAgenciji);
    }

    [Fact]
    public void DjelimicnoVraceniIznosSeSamoDopunjava()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(
            Ulaz(danaDoPreuzimanja: 20, vecVraceno: 120m));

        Assert.Equal(380m, obracun.UkupanPovrat);
    }

    /// <summary>
    /// Ako je naplaceno manje nego sto depozit iznosi, ne moze se vratiti vise nego
    /// sto je uzeto. Ovo je jedini slucaj u kojem depozit nije cijeli.
    /// </summary>
    [Fact]
    public void PovratNikadNijeVeciOdNaplacenog()
    {
        var obracun = PravilaOtkazivanja.Izracunaj(
            Ulaz(danaDoPreuzimanja: 1, naplaceno: 60m, depozit: 100m));

        Assert.Equal(60m, obracun.DioDepozita);
        Assert.Equal(0m, obracun.DioNajma);
        Assert.Equal(60m, obracun.UkupanPovrat);
        Assert.Equal(0m, obracun.ZadrzanoAgenciji);
    }

    [Fact]
    public void IznosiSeZaokruzujuNaDvijeDecimale()
    {
        // Najam 333.33, polovina je 166.665 - zaokruzuje se dalje od nule, na 166.67.
        var obracun = PravilaOtkazivanja.Izracunaj(
            Ulaz(danaDoPreuzimanja: 4, naplaceno: 433.33m, depozit: 100m));

        Assert.Equal(333.33m, obracun.DioNajma);
        Assert.Equal(166.67m, obracun.PovratNajma);
        Assert.Equal(266.67m, obracun.UkupanPovrat);

        // Razrada se mora sabirati u naplaceno: povrat plus zadrzano.
        Assert.Equal(obracun.Naplaceno, obracun.UkupanPovrat + obracun.ZadrzanoAgenciji);
    }
}
