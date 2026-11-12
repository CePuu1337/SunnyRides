using SunnyRides.Services.Oprema;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Koliko komada opreme rezervacije drze u isto vrijeme. Primjer:
/// poslovnica ima dvije kacige, dvije su vec rezervisane za isti vikend, pa treca
/// rezervacija za taj vikend ne smije proci.
/// </summary>
public class IstovremenaOpremaTests
{
    private static readonly DateTime Petak = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static Zaduzenje Dana(int odDana, int doDana, int kolicina) =>
        new(Petak.AddDays(odDana), Petak.AddDays(doDana), kolicina);

    [Fact]
    public void Dvije_rezervacije_za_isti_vikend_drze_dvije_kacige()
    {
        var vrhunac = IstovremenaOprema.Najvise(new[] { Dana(0, 2, 1), Dana(0, 2, 1) });

        Assert.Equal(2, vrhunac.Kolicina);
        Assert.Equal(Petak, vrhunac.Od);
    }

    [Fact]
    public void Rezervacije_koje_se_medjusobno_ne_preklapaju_dijele_isti_komad()
    {
        // Ponedjeljak-srijeda i cetvrtak-subota: jedna kaciga pokriva obje.
        var vrhunac = IstovremenaOprema.Najvise(new[] { Dana(3, 5, 1), Dana(6, 8, 1) });

        Assert.Equal(1, vrhunac.Kolicina);
    }

    [Fact]
    public void Vracanje_i_preuzimanje_u_istom_trenutku_se_ne_preklapaju()
    {
        var vrhunac = IstovremenaOprema.Najvise(new[] { Dana(0, 2, 1), Dana(2, 4, 1) });

        Assert.Equal(1, vrhunac.Kolicina);
    }

    [Fact]
    public void Kolicine_se_sabiraju_samo_dok_traju_zajedno()
    {
        // 2 kom. cijelu sedmicu, 3 kom. od utorka do cetvrtka, 1 kom. vikend poslije.
        var vrhunac = IstovremenaOprema.Najvise(new[] { Dana(0, 7, 2), Dana(4, 6, 3), Dana(8, 10, 1) });

        Assert.Equal(5, vrhunac.Kolicina);
        Assert.Equal(Petak.AddDays(4), vrhunac.Od);
    }

    [Fact]
    public void Bez_rezervacija_nista_nije_zauzeto()
    {
        var vrhunac = IstovremenaOprema.Najvise(Array.Empty<Zaduzenje>());

        Assert.Equal(0, vrhunac.Kolicina);
        Assert.Null(vrhunac.Od);
    }

    [Fact]
    public void Treca_kaciga_za_isti_vikend_se_odbija_sa_brojem_slobodnih()
    {
        var razlog = IstovremenaOprema.Razlog("Kaciga", "SunnyRides Mostar", naStanju: 2, zauzeto: 2, trazeno: 1);

        Assert.NotNull(razlog);
        Assert.Contains("vec rezervisana", razlog);
    }

    [Fact]
    public void Kad_je_slobodno_manje_od_trazenog_poruka_kaze_koliko_je_slobodno()
    {
        var razlog = IstovremenaOprema.Razlog("Kaciga", "SunnyRides Mostar", naStanju: 3, zauzeto: 2, trazeno: 2);

        Assert.NotNull(razlog);
        Assert.Contains("slobodno jos 1 kom.", razlog);
    }

    [Fact]
    public void Oprema_bez_zaliha_u_poslovnici_nije_dostupna()
    {
        var razlog = IstovremenaOprema.Razlog("GPS uredjaj", "SunnyRides Neum", naStanju: 0, zauzeto: 0, trazeno: 1);

        Assert.Equal("Oprema \"GPS uredjaj\" nije dostupna u poslovnici SunnyRides Neum.", razlog);
    }

    [Fact]
    public void Dovoljno_slobodnih_prolazi()
    {
        Assert.Null(IstovremenaOprema.Razlog("Kaciga", "SunnyRides Mostar", naStanju: 5, zauzeto: 3, trazeno: 2));
    }
}
