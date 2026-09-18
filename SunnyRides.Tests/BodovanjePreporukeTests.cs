using SunnyRides.Services.Preporuke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Model preporuke. Testovi drze na okupu dvije stvari: da tezine iz dokumentacije
/// stvarno vrijede, i da signal koji se ispise korisniku bude onaj koji je vozilo
/// zaista doveo na vrh.
/// </summary>
public class BodovanjePreporukeTests
{
    private static readonly KontekstPopularnosti Kontekst = new(NajviseNajmova: 10, ProsjekOcjenaFlote: 4.0);

    private static KandidatVozilo Vozilo(
        int tip = 1, int marka = 1, int grad = 1, int kubikaza = 125, decimal tarifa = 40m) =>
        new(VoziloId: 1, ModelVozilaId: 1, TipVozilaId: tip, MarkaId: marka,
            GradId: grad, Kubikaza: kubikaza, DnevnaTarifa: tarifa);

    private static ProfilKorisnika Profil(
        (int Vrijednost, double Udio)[]? tipovi = null,
        (int Vrijednost, double Udio)[]? marke = null,
        (int Vrijednost, double Udio)[]? gradovi = null,
        (int Vrijednost, double Udio)[]? razredi = null,
        decimal? cijena = null) =>
        new(Rjecnik(tipovi), Rjecnik(marke), Rjecnik(gradovi), Rjecnik(razredi), cijena);

    private static IReadOnlyDictionary<int, double> Rjecnik((int Vrijednost, double Udio)[]? stavke) =>
        stavke?.ToDictionary(x => x.Vrijednost, x => x.Udio) ?? new Dictionary<int, double>();

    // --- razredi kubikaze --------------------------------------------------

    [Theory]
    [InlineData(49, 1)]
    [InlineData(50, 1)]
    [InlineData(51, 2)]
    [InlineData(125, 2)]
    [InlineData(126, 3)]
    [InlineData(500, 3)]
    [InlineData(501, 4)]
    public void RazredKubikaze(int kubikaza, int ocekivano)
    {
        Assert.Equal(ocekivano, BodovanjePreporuke.RazredKubikaze(kubikaza));
    }

    // --- popularnost -------------------------------------------------------

    [Fact]
    public void NajtrazenijeVoziloImaUcestalostJedan()
    {
        var p = BodovanjePreporuke.Popularnost(new SignaliPopularnosti(10, 0, 0), Kontekst);

        Assert.Equal(1, p.Ucestalost, 6);
    }

    [Fact]
    public void BezNajmovaUFlotiUcestalostJeNula()
    {
        var p = BodovanjePreporuke.Popularnost(
            new SignaliPopularnosti(0, 0, 0), new KontekstPopularnosti(0, 4.0));

        Assert.Equal(0, p.Ucestalost, 6);
    }

    [Fact]
    public void VoziloBezRecenzijaDobijaProsjekFlote()
    {
        var p = BodovanjePreporuke.Popularnost(new SignaliPopularnosti(0, 0, 0), Kontekst);

        Assert.Equal(4.0, p.Bayes, 6);
    }

    /// <summary>
    /// Ovo je razlog zbog kojeg je ocjena Bayesova, a ne obicni prosjek: jedna petica
    /// ne smije preskociti vozilo sa mnogo ocjena koje su tek malo nize.
    /// </summary>
    [Fact]
    public void JednaPeticaNePreskaceVoziloSaMnogoOcjena()
    {
        var jedna = BodovanjePreporuke.Popularnost(new SignaliPopularnosti(0, 1, 5), Kontekst);
        var mnogo = BodovanjePreporuke.Popularnost(new SignaliPopularnosti(0, 40, 4.7 * 40), Kontekst);

        Assert.True(jedna.Bayes < mnogo.Bayes);
        Assert.Equal(4.1667, jedna.Bayes, 3);
    }

    [Fact]
    public void FlotaBezRecenzijaKoristiNeutralnuOcjenu()
    {
        var p = BodovanjePreporuke.Popularnost(
            new SignaliPopularnosti(0, 0, 0), new KontekstPopularnosti(5, 0));

        Assert.Equal(BodovanjePreporuke.NeutralnaOcjena, p.Bayes, 6);
    }

    // --- slicnost ----------------------------------------------------------

    [Fact]
    public void PotpunoPoklapanjeDajeSlicnostJedan()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(
                tipovi: new[] { (1, 1.0) },
                marke: new[] { (1, 1.0) },
                gradovi: new[] { (1, 1.0) },
                razredi: new[] { (2, 1.0) },
                cijena: 40m),
            Vozilo(),
            SignaliPopularnosti.Prazni,
            Kontekst);

        Assert.Equal(1, r.Slicnost, 6);
    }

    [Fact]
    public void NikakvoPoklapanjeDajeSlicnostNula()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(
                tipovi: new[] { (9, 1.0) },
                marke: new[] { (9, 1.0) },
                gradovi: new[] { (9, 1.0) },
                razredi: new[] { (4, 1.0) },

                // Vozilo je dvostruko skuplje od onoga sto korisnik trazi, a to je
                // tacka na kojoj poklapanje cijene pada na nulu.
                cijena: 20m),
            Vozilo(),
            SignaliPopularnosti.Prazni,
            Kontekst);

        Assert.Equal(0, r.Slicnost, 6);
    }

    /// <summary>
    /// Signal o kojem profil nema podatak ne smije spustiti slicnost. Korisnik koji je
    /// trazio samo tip vozila, i to poklopljen, mora imati slicnost 1 - a ne 0,35.
    /// </summary>
    [Fact]
    public void NepoznatSignalNeUlaziURacun()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (1, 1.0) }),
            Vozilo(),
            SignaliPopularnosti.Prazni,
            Kontekst);

        Assert.Equal(1, r.Slicnost, 6);
    }

    [Fact]
    public void TipNosiVeciUdioOdMarke()
    {
        var samoTip = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (1, 1.0) }, marke: new[] { (9, 1.0) }),
            Vozilo(tip: 1, marka: 1),
            SignaliPopularnosti.Prazni, Kontekst);

        var samoMarka = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (9, 1.0) }, marke: new[] { (1, 1.0) }),
            Vozilo(tip: 1, marka: 1),
            SignaliPopularnosti.Prazni, Kontekst);

        Assert.True(samoTip.Slicnost > samoMarka.Slicnost);
        Assert.Equal(BodovanjePreporuke.TezinaTip / (BodovanjePreporuke.TezinaTip + BodovanjePreporuke.TezinaMarka),
                     samoTip.Slicnost, 6);
    }

    [Theory]
    [InlineData(40, 1.0)]     // tacno onoliko koliko korisnik trazi
    [InlineData(50, 0.75)]    // cetvrtina skuplje
    [InlineData(80, 0.0)]     // dvostruko skuplje - poklapanje pada na nulu
    [InlineData(200, 0.0)]    // i dalje nula, nikad negativno
    public void BlizinaCijenePadaSaOdstupanjem(int tarifa, double ocekivano)
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(cijena: 40m), Vozilo(tarifa: tarifa),
            SignaliPopularnosti.Prazni, Kontekst);

        Assert.Equal(ocekivano, r.Slicnost, 6);
    }

    // --- konacni skor ------------------------------------------------------

    [Fact]
    public void SkorJeSestDesetinaSlicnostiICetiriDesetinePopularnosti()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (1, 1.0) }),
            Vozilo(),
            new SignaliPopularnosti(10, 0, 0),
            Kontekst);

        var ocekivano = 0.6 * r.Slicnost + 0.4 * r.Popularnost;

        Assert.Equal(ocekivano, r.Skor, 6);
        Assert.Equal(0.6, BodovanjePreporuke.UdioSlicnosti, 6);
        Assert.Equal(0.4, BodovanjePreporuke.UdioPopularnosti, 6);
    }

    /// <summary>
    /// Novi korisnik nema profil, pa skor mora biti cista popularnost. Kad bi se i
    /// tada mnozilo sa 0,4, svi bi skorovi bili nizi a poredak isti - brojevi bi
    /// tvrdili nesto sto model ne radi.
    /// </summary>
    [Fact]
    public void KorisnikBezHistorijeDobijaCistuPopularnost()
    {
        var r = BodovanjePreporuke.Boduj(
            ProfilKorisnika.Prazan, Vozilo(), new SignaliPopularnosti(5, 4, 20), Kontekst);

        Assert.Equal(0, r.Slicnost, 6);
        Assert.Equal(r.Popularnost, r.Skor, 6);
    }

    [Fact]
    public void ZbirTezinaJeJedan()
    {
        var zbir = BodovanjePreporuke.TezinaTip
                   + BodovanjePreporuke.TezinaCijena
                   + BodovanjePreporuke.TezinaLokacija
                   + BodovanjePreporuke.TezinaMarka
                   + BodovanjePreporuke.TezinaKubikaza;

        Assert.Equal(1.0, zbir, 6);
    }

    // --- obrazlozenje ------------------------------------------------------

    [Fact]
    public void NajjaciSignalIzProfilaUlaziUObrazlozenje()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (1, 1.0) }, marke: new[] { (1, 1.0) }),
            Vozilo(tip: 1, marka: 1),
            SignaliPopularnosti.Prazni,
            Kontekst);

        Assert.Equal(SignalPreporuke.Tip, r.Signal);
    }

    [Fact]
    public void BezProfilaObrazlozenjeDolaziIzPopularnosti()
    {
        var r = BodovanjePreporuke.Boduj(
            ProfilKorisnika.Prazan, Vozilo(), new SignaliPopularnosti(10, 0, 0), Kontekst);

        Assert.Equal(SignalPreporuke.Popularnost, r.Signal);
    }

    /// <summary>
    /// Vozilo bez ijednog najma, ali sa odlicnim ocjenama, mora se braniti ocjenom -
    /// "najtrazenije" bi u tom slucaju bila neistina.
    /// </summary>
    [Fact]
    public void VoziloBezNajmovaSaDobrimOcjenamaBraniSeOcjenom()
    {
        var r = BodovanjePreporuke.Boduj(
            ProfilKorisnika.Prazan, Vozilo(), new SignaliPopularnosti(0, 30, 5 * 30), Kontekst);

        Assert.Equal(SignalPreporuke.Ocjena, r.Signal);
    }

    /// <summary>
    /// Popularnost ne smije pregaziti profil samo zato sto joj je sirova vrijednost
    /// veca - u skor ulazi sa udjelom 0,4, pa se i poredi tako.
    /// </summary>
    [Fact]
    public void PopularnostSeUporedjujeTekPomnozenaSvojimUdjelom()
    {
        var r = BodovanjePreporuke.Boduj(
            Profil(tipovi: new[] { (1, 1.0) }),
            Vozilo(tip: 1),
            new SignaliPopularnosti(10, 0, 0),
            Kontekst);

        // popularnost je visoka (0,875), ali 0,4 x 0,875 < 0,6 x 1
        Assert.Equal(SignalPreporuke.Tip, r.Signal);
    }
}
