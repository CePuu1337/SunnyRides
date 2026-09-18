using SunnyRides.Services.Preporuke.Ml;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Priprema podataka za model. Ovo je najosjetljiviji dio sistema preporuke: greska
/// ovdje ne obara build nego tiho pokvari ono sto model nauci, a to se na izlazu vidi
/// tek kao "preporuke su nekako cudne".
/// </summary>
public class PripremaInterakcijaTests
{
    private const double ProsjekFlote = 4.0;

    private static List<InterakcijaZapis> Sastavi(
        OcjenaZapis[]? ocjene = null, NajamZapis[]? najmovi = null) =>
        PripremaInterakcija.Sastavi(
            ocjene ?? Array.Empty<OcjenaZapis>(),
            najmovi ?? Array.Empty<NajamZapis>(),
            ProsjekFlote);

    [Fact]
    public void OcjenaPostajeRedSaOznakomStvarne()
    {
        var redovi = Sastavi(ocjene: new[] { new OcjenaZapis(1, 10, 5) });

        var red = Assert.Single(redovi);
        Assert.Equal(1, red.KorisnikId);
        Assert.Equal(10, red.ModelVozilaId);
        Assert.Equal(5, red.Ocjena);
        Assert.True(red.JeStvarnaOcjena);
    }

    /// <summary>
    /// Isti korisnik moze iznajmiti isti model vise puta i svaki put ga ocijeniti.
    /// Matrica ima jedno polje po paru, pa te ocjene moraju ući kao prosjek.
    /// </summary>
    [Fact]
    public void ViseOcjenaIstogParaUlaziKaoProsjek()
    {
        var redovi = Sastavi(ocjene: new[]
        {
            new OcjenaZapis(1, 10, 5),
            new OcjenaZapis(1, 10, 3)
        });

        var red = Assert.Single(redovi);
        Assert.Equal(4, red.Ocjena);
    }

    [Fact]
    public void NajamBezRecenzijeUlaziSaProsjekomFloteIBezOznakeStvarne()
    {
        var redovi = Sastavi(najmovi: new[] { new NajamZapis(1, 10) });

        var red = Assert.Single(redovi);
        Assert.Equal(ProsjekFlote, red.Ocjena);
        Assert.False(red.JeStvarnaOcjena);
    }

    /// <summary>
    /// Kad par ima i ocjenu i najam, vrijedi ocjena. Procijenjena vrijednost ne smije
    /// pregaziti ono sto je korisnik stvarno rekao.
    /// </summary>
    [Fact]
    public void OcjenaImaPrednostNadProcjenom()
    {
        var redovi = Sastavi(
            ocjene: new[] { new OcjenaZapis(1, 10, 2) },
            najmovi: new[] { new NajamZapis(1, 10) });

        var red = Assert.Single(redovi);
        Assert.Equal(2, red.Ocjena);
        Assert.True(red.JeStvarnaOcjena);
    }

    [Fact]
    public void ViseNajmovaIstogParaDajeJedanRed()
    {
        var redovi = Sastavi(najmovi: new[]
        {
            new NajamZapis(1, 10),
            new NajamZapis(1, 10),
            new NajamZapis(1, 11)
        });

        Assert.Equal(2, redovi.Count);
    }

    // --- prag za treniranje ------------------------------------------------

    [Fact]
    public void MaloOcjenaNijeDovoljnoZaTreniranje()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 5, poKorisniku: 2));  // 10 ocjena

        Assert.False(PripremaInterakcija.DovoljnoZaTreniranje(redovi));
    }

    [Fact]
    public void OcjeneJednogKorisnikaNisuDovoljneMakarIhBiloMnogo()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 1, poKorisniku: 40));

        Assert.False(PripremaInterakcija.DovoljnoZaTreniranje(redovi));
    }

    [Fact]
    public void DovoljnoOcjenaIKorisnikaOtvaraTreniranje()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 5, poKorisniku: 4));  // 20 ocjena

        Assert.True(PripremaInterakcija.DovoljnoZaTreniranje(redovi));
    }

    /// <summary>Procijenjeni redovi ne smiju popuniti prag umjesto stvarnih ocjena.</summary>
    [Fact]
    public void ProcijenjeniRedoviSeNeRacunajuUPrag()
    {
        var redovi = Sastavi(
            ocjene: Ocjene(brojKorisnika: 3, poKorisniku: 2),      // 6 stvarnih
            najmovi: Enumerable.Range(1, 40)
                .Select(i => new NajamZapis(100 + i, 200 + i)).ToArray());

        Assert.False(PripremaInterakcija.DovoljnoZaTreniranje(redovi));
    }

    // --- podjela -----------------------------------------------------------

    [Fact]
    public void UProvjeruIduSamoStvarneOcjene()
    {
        var redovi = Sastavi(
            ocjene: Ocjene(brojKorisnika: 10, poKorisniku: 5),     // 50 stvarnih
            najmovi: Enumerable.Range(1, 20)
                .Select(i => new NajamZapis(500 + i, 600 + i)).ToArray());

        var (ucenje, provjera) = PripremaInterakcija.Podijeli(redovi);

        Assert.All(provjera, x => Assert.True(x.JeStvarnaOcjena));
        Assert.Equal(20, ucenje.Count(x => !x.JeStvarnaOcjena));
        Assert.Equal(redovi.Count, ucenje.Count + provjera.Count);
    }

    [Fact]
    public void PodjelaJeUvijekIsta()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 10, poKorisniku: 5));

        var prva = PripremaInterakcija.Podijeli(redovi).ZaProvjeru.Select(x => x.KorisnikId).ToList();
        var druga = PripremaInterakcija.Podijeli(redovi).ZaProvjeru.Select(x => x.KorisnikId).ToList();

        Assert.Equal(prva, druga);
    }

    /// <summary>
    /// Kad ocjena ima taman koliko treba za ucenje, test skup ostaje prazan - bolje je
    /// ne izmjeriti gresku nego uciti na premalo podataka da bi se ona izmjerila.
    /// </summary>
    [Fact]
    public void PodjelaNeSmijeOstavitiPremaloZaUcenje()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 5, poKorisniku: 3));  // 15 ocjena

        var (ucenje, provjera) = PripremaInterakcija.Podijeli(redovi);

        Assert.Empty(provjera);
        Assert.Equal(PripremaInterakcija.NajmanjeOcjenaZaTreniranje, ucenje.Count);
    }
    // --- unakrsna provjera -------------------------------------------------

    [Fact]
    public void SvakaOcjenaTacnoJednomBudeUProvjeri()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 10, poKorisniku: 5));  // 50 stvarnih

        var dijelovi = PripremaInterakcija.PodijeliUnakrsno(redovi, brojDijelova: 5);

        Assert.Equal(5, dijelovi.Count);

        var uProvjeri = dijelovi.SelectMany(x => x.ZaProvjeru).ToList();

        Assert.Equal(50, uProvjeri.Count);
        Assert.Equal(50, uProvjeri.Distinct().Count());
    }

    [Fact]
    public void ProcijenjeniRedoviUvijekIduUUcenje()
    {
        var redovi = Sastavi(
            ocjene: Ocjene(brojKorisnika: 10, poKorisniku: 5),
            najmovi: Enumerable.Range(1, 12)
                .Select(i => new NajamZapis(500 + i, 600 + i)).ToArray());

        var dijelovi = PripremaInterakcija.PodijeliUnakrsno(redovi);

        Assert.All(dijelovi, dio =>
        {
            Assert.All(dio.ZaProvjeru, x => Assert.True(x.JeStvarnaOcjena));
            Assert.Equal(12, dio.ZaUcenje.Count(x => !x.JeStvarnaOcjena));
        });
    }

    /// <summary>
    /// Kad bi dijeljenje ostavilo premalo ocjena za ucenje, unakrsne provjere nema -
    /// bolje bez nje nego sa greskom izmjerenom na modelu koji nije mogao nista nauciti.
    /// </summary>
    [Fact]
    public void PremaloOcjenaZnaciDaUnakrsneProvjereNema()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 4, poKorisniku: 4));  // 16 stvarnih

        Assert.Empty(PripremaInterakcija.PodijeliUnakrsno(redovi, brojDijelova: 5));
    }

    [Fact]
    public void PodjelaNaDijeloveJeUvijekIsta()
    {
        var redovi = Sastavi(ocjene: Ocjene(brojKorisnika: 10, poKorisniku: 5));

        var prva = PripremaInterakcija.PodijeliUnakrsno(redovi)[0].ZaProvjeru.Count;
        var druga = PripremaInterakcija.PodijeliUnakrsno(redovi)[0].ZaProvjeru.Count;

        Assert.Equal(prva, druga);
    }

    private static OcjenaZapis[] Ocjene(int brojKorisnika, int poKorisniku) =>
        Enumerable.Range(1, brojKorisnika)
            .SelectMany(korisnik => Enumerable.Range(1, poKorisniku)
                .Select(model => new OcjenaZapis(korisnik, model, 3 + (korisnik + model) % 3)))
            .ToArray();
}
