using SunnyRides.Services.Auth;

namespace SunnyRides.Tests;

/// <summary>
/// Kod za reset lozinke je jedina stvar koja u tom trenutku stoji izmedju napadaca i
/// tudjeg naloga, pa se provjerava i njegov oblik i to sto se prepisan rukom prizna.
/// </summary>
public class KodoviZaResetTests
{
    private const string Abeceda = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [Fact]
    public void Kod_ima_dogovorenu_duzinu()
    {
        var kod = KodoviZaReset.Generisi();

        Assert.Equal(KodoviZaReset.Duzina, kod.Length);
    }

    [Fact]
    public void Kod_koristi_samo_znakove_iz_abecede()
    {
        // Znakovi koji se mijesaju pri prepisivanju (I, O, 0, 1) ne smiju se pojaviti.
        for (var i = 0; i < 200; i++)
        {
            var kod = KodoviZaReset.Generisi();

            Assert.All(kod, znak => Assert.Contains(znak, Abeceda));
        }
    }

    [Fact]
    public void Dva_uzastopna_koda_nisu_ista()
    {
        var kodovi = Enumerable.Range(0, 100).Select(_ => KodoviZaReset.Generisi()).ToList();

        Assert.Equal(kodovi.Count, kodovi.Distinct().Count());
    }

    [Fact]
    public void Generisan_kod_prolazi_provjeru_oblika()
    {
        var kod = KodoviZaReset.Generisi();

        Assert.True(KodoviZaReset.JeMogucOblik(kod));
    }

    [Theory]
    [InlineData("abcdefgh", "ABCDEFGH")]
    [InlineData("ABCD EFGH", "ABCDEFGH")]
    [InlineData("ABCD-EFGH", "ABCDEFGH")]
    [InlineData("  abcd-efgh  ", "ABCDEFGH")]
    public void Kod_se_prima_i_kad_je_prepisan_sa_razmakom_ili_malim_slovima(string uneseno, string ocekivano)
    {
        Assert.Equal(ocekivano, KodoviZaReset.Normalizuj(uneseno));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Prazan_unos_daje_prazan_kod(string? uneseno)
    {
        Assert.Equal(string.Empty, KodoviZaReset.Normalizuj(uneseno));
    }

    [Theory]
    [InlineData("ABCDEFG")]      // prekratak
    [InlineData("ABCDEFGHI")]    // predugacak
    [InlineData("ABCDEFG0")]     // nula nije u abecedi
    [InlineData("ABCDEFGI")]     // slovo I nije u abecedi
    [InlineData("")]
    public void Kod_pogresnog_oblika_se_odbija_bez_upita_u_bazu(string kod)
    {
        Assert.False(KodoviZaReset.JeMogucOblik(kod));
    }

    [Fact]
    public void Kod_vazi_petnaest_minuta()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), KodoviZaReset.Trajanje);
    }
}
