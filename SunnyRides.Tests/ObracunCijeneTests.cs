using SunnyRides.Services.Cijene;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Sastavljanje cijene: sezonski mnozilac, popust, oprema, osiguranje i depozit.
/// Nijedan test ne dira bazu - obracun prima gotove brojeve i vraca razradu.
/// </summary>
public class ObracunCijeneTests
{
    private static readonly DateTime Pocetak = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>Tarife su okrugle namjerno, da se ocekivani iznos moze provjeriti napamet.</summary>
    private static UlazObracuna Ulaz(
        DateTime datumDo,
        decimal mnozilac = 1m,
        IReadOnlyList<StavkaOpremeUlaz>? oprema = null,
        int? paketOsiguranjaId = null,
        decimal osiguranjePoDanu = 0m,
        decimal depozit = 0m) =>
        new(
            DatumOd: Pocetak,
            DatumDo: datumDo,
            SatnaTarifa: 6m,
            DnevnaTarifa: 30m,
            Mnozilac: mnozilac,
            NazivSezone: mnozilac == 1m ? null : "Glavna sezona",
            PopustPrag1: 3,
            PopustProcenat1: 5m,
            PopustPrag2: 7,
            PopustProcenat2: 10m,
            IznosDepozita: depozit,
            Oprema: oprema ?? Array.Empty<StavkaOpremeUlaz>(),
            PaketOsiguranjaId: paketOsiguranjaId,
            PaketOsiguranjaNaziv: paketOsiguranjaId.HasValue ? "Puni kasko" : null,
            OsiguranjeCijenaPoDanu: osiguranjePoDanu);

    [Fact]
    public void NajamPoSatu_KoristiSatnuTarifu()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddHours(5)));

        Assert.True(cijena.NaplataPoSatu);
        Assert.Equal(5, cijena.BrojSati);
        Assert.Equal(30m, cijena.OsnovicaNajma);   // 6 x 5
        Assert.Equal(0m, cijena.ProcenatPopusta);
        Assert.Equal(30m, cijena.UkupanIznos);
    }

    [Fact]
    public void DvaDana_NemaPopusta()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(2)));

        Assert.Equal(2, cijena.BrojDana);
        Assert.Equal(60m, cijena.OsnovicaNajma);
        Assert.Equal(0m, cijena.IznosPopusta);
        Assert.Equal(60m, cijena.IznosNajma);
    }

    [Fact]
    public void TriDana_DobijaPrviPrag()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(3)));

        Assert.Equal(5m, cijena.ProcenatPopusta);
        Assert.Equal(90m, cijena.OsnovicaNajma);
        Assert.Equal(4.50m, cijena.IznosPopusta);
        Assert.Equal(85.50m, cijena.IznosNajma);
    }

    [Fact]
    public void SedamDana_DobijaDrugiPrag()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(7)));

        Assert.Equal(10m, cijena.ProcenatPopusta);
        Assert.Equal(210m, cijena.OsnovicaNajma);
        Assert.Equal(21m, cijena.IznosPopusta);
        Assert.Equal(189m, cijena.IznosNajma);
    }

    [Fact]
    public void DesetDana_DobijaVisiPragANeNizi()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(10)));

        Assert.Equal(10m, cijena.ProcenatPopusta);
    }

    [Fact]
    public void SezonskiMnozilac_DjelujePrijePopusta()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(7), mnozilac: 1.30m));

        // 30 x 7 = 210 osnovica; x 1,30 = 273; popust 10 % = 27,30; najam = 245,70
        Assert.Equal(210m, cijena.OsnovicaNajma);
        Assert.Equal(27.30m, cijena.IznosPopusta);
        Assert.Equal(245.70m, cijena.IznosNajma);
        Assert.Equal("Glavna sezona", cijena.NazivSezone);
    }

    [Fact]
    public void OpremaPoDanu_MnoziSeBrojemDanaIKolicinom()
    {
        var kaciga = new StavkaOpremeUlaz(1, "Kaciga", Kolicina: 2, CijenaPoDanu: 3m, FiksnaCijena: null);

        var cijena = ObracunCijene.Izracunaj(
            Ulaz(Pocetak.AddDays(7), oprema: new[] { kaciga }));

        var stavka = Assert.Single(cijena.Oprema);
        Assert.Equal(21m, stavka.CijenaPoJedinici);   // 3 x 7 dana
        Assert.Equal(42m, stavka.Iznos);              // x 2 komada
        Assert.Equal(42m, cijena.IznosOpreme);
    }

    [Fact]
    public void FiksnaCijenaOpreme_NeZavisiOdTrajanja()
    {
        var kofer = new StavkaOpremeUlaz(2, "Top-case kofer", Kolicina: 1, CijenaPoDanu: null, FiksnaCijena: 20m);

        var kratko = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(1), oprema: new[] { kofer }));
        var dugo = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(10), oprema: new[] { kofer }));

        Assert.Equal(20m, kratko.IznosOpreme);
        Assert.Equal(20m, dugo.IznosOpreme);
    }

    [Fact]
    public void OpremaNaNajmuPoSatu_NaplacujeSeKaoJedanDan()
    {
        var gps = new StavkaOpremeUlaz(3, "GPS", Kolicina: 1, CijenaPoDanu: 4m, FiksnaCijena: null);

        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddHours(5), oprema: new[] { gps }));

        Assert.Equal(4m, cijena.IznosOpreme);
    }

    [Fact]
    public void Osiguranje_SeNaplacujePoDanu()
    {
        var cijena = ObracunCijene.Izracunaj(
            Ulaz(Pocetak.AddDays(7), paketOsiguranjaId: 2, osiguranjePoDanu: 5m));

        Assert.Equal(35m, cijena.IznosOsiguranja);
        Assert.Equal("Puni kasko", cijena.PaketOsiguranjaNaziv);
    }

    [Fact]
    public void BezPaketaOsiguranja_IznosJeNula()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(7), osiguranjePoDanu: 5m));

        Assert.Equal(0m, cijena.IznosOsiguranja);
        Assert.Null(cijena.PaketOsiguranjaNaziv);
    }

    [Fact]
    public void Depozit_UlaziUUkupanIznosKaoZasebnaStavka()
    {
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(2), depozit: 150m));

        Assert.Equal(150m, cijena.IznosDepozita);
        Assert.Equal(210m, cijena.UkupanIznos);   // 60 najam + 150 depozit
    }

    [Fact]
    public void RazradaSeSabiraUUkupanIznos()
    {
        var kaciga = new StavkaOpremeUlaz(1, "Kaciga", Kolicina: 2, CijenaPoDanu: 3m, FiksnaCijena: null);

        var cijena = ObracunCijene.Izracunaj(Ulaz(
            Pocetak.AddDays(7),
            mnozilac: 1.30m,
            oprema: new[] { kaciga },
            paketOsiguranjaId: 2,
            osiguranjePoDanu: 5m,
            depozit: 150m));

        // 245,70 najam + 42 oprema + 35 osiguranje + 150 depozit = 472,70
        Assert.Equal(472.70m, cijena.UkupanIznos);

        var zbir = cijena.IznosNajma + cijena.IznosOpreme + cijena.IznosOsiguranja + cijena.IznosDepozita;
        Assert.Equal(cijena.UkupanIznos, zbir);
    }

    [Fact]
    public void IznosiSuZaokruzeniNaDvijeDecimale()
    {
        // 30 x 3 = 90; x 1,17 = 105,30; popust 5 % = 5,265 -> 5,27
        var cijena = ObracunCijene.Izracunaj(Ulaz(Pocetak.AddDays(3), mnozilac: 1.17m));

        Assert.Equal(5.27m, cijena.IznosPopusta);
        Assert.Equal(100.03m, cijena.IznosNajma);
    }
}
