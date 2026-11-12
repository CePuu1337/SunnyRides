using SunnyRides.Services.Izvjestaji;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Redovi finansijskog pregleda. Primjer: najam placen u septembru i
/// otkazan u oktobru, bez drugih naplata te poslovnice u oktobru - povrat mora imati
/// svoj red u oktobru i uci u zbir.
/// </summary>
public class RedoviFinansijskogTests
{
    [Fact]
    public void Mjesec_samo_sa_povratom_dobija_svoj_red_i_ulazi_u_zbir()
    {
        var naplate = new[] { new NaplataMjeseca(2026, 9, 1, "SunnyRides Mostar", 1, 160m, 60m) };
        var povrati = new[] { new PovratMjeseca(2026, 10, 1, "SunnyRides Mostar", 160m) };

        var (stavke, zbir) = RedoviFinansijskog.Sastavi(naplate, povrati);

        Assert.Equal(2, stavke.Count);

        var oktobar = stavke[1];
        Assert.Equal("oktobar 2026", oktobar.Period);
        Assert.Equal(0m, oktobar.Naplaceno);
        Assert.Equal(160m, oktobar.Refundirano);
        Assert.Equal(-160m, oktobar.NetoPrihod);
        Assert.Equal(0, oktobar.BrojRezervacija);

        Assert.Equal(160m, zbir.Refundirano);
        Assert.Equal(0m, zbir.NetoPrihod);
    }

    [Fact]
    public void Naplata_i_povrat_istog_mjeseca_su_jedan_red()
    {
        var naplate = new[] { new NaplataMjeseca(2026, 10, 2, "SunnyRides Split", 2, 400m, 200m) };
        var povrati = new[] { new PovratMjeseca(2026, 10, 2, "SunnyRides Split", 150m) };

        var (stavke, _) = RedoviFinansijskog.Sastavi(naplate, povrati);

        var red = Assert.Single(stavke);
        Assert.Equal(400m, red.Naplaceno);
        Assert.Equal(150m, red.Refundirano);
        Assert.Equal(250m, red.NetoPrihod);
    }

    [Fact]
    public void Prosjek_najma_ne_ukljucuje_depozit()
    {
        // Dva najma po 60 EUR, svaki uz 100 EUR depozita: naplaceno 320, prosjek 60.
        var naplate = new[] { new NaplataMjeseca(2026, 10, 1, "SunnyRides Mostar", 2, 320m, 120m) };

        var (stavke, zbir) = RedoviFinansijskog.Sastavi(naplate, Array.Empty<PovratMjeseca>());

        Assert.Equal(60m, stavke[0].ProsjecnaVrijednostNajma);
        Assert.Equal(60m, zbir.ProsjecnaVrijednostNajma);
    }

    [Fact]
    public void Prosjek_u_zbiru_je_po_najmu_a_ne_po_redu()
    {
        var naplate = new[]
        {
            new NaplataMjeseca(2026, 9, 1, "SunnyRides Mostar", 3, 0m, 300m),
            new NaplataMjeseca(2026, 10, 1, "SunnyRides Mostar", 1, 0m, 20m)
        };

        var (_, zbir) = RedoviFinansijskog.Sastavi(naplate, Array.Empty<PovratMjeseca>());

        Assert.Equal(80m, zbir.ProsjecnaVrijednostNajma);
    }

    [Fact]
    public void Redovi_su_poredani_po_mjesecu_pa_po_poslovnici()
    {
        var naplate = new[]
        {
            new NaplataMjeseca(2026, 10, 2, "SunnyRides Split", 1, 10m, 10m),
            new NaplataMjeseca(2026, 9, 1, "SunnyRides Mostar", 1, 10m, 10m)
        };
        var povrati = new[] { new PovratMjeseca(2026, 10, 1, "SunnyRides Mostar", 5m) };

        var (stavke, _) = RedoviFinansijskog.Sastavi(naplate, povrati);

        Assert.Equal(
            new[] { "septembar 2026 SunnyRides Mostar", "oktobar 2026 SunnyRides Mostar", "oktobar 2026 SunnyRides Split" },
            stavke.Select(x => $"{x.Period} {x.Poslovnica}"));
    }
}
