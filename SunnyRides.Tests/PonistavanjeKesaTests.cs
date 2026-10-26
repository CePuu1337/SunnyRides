using Microsoft.Extensions.Caching.Memory;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Flota;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Kes pravila kategorija i cjenovnika mora nestati cim se podatak promijeni. Ako ne
/// nestane, izmjena u sifarniku do 15 minuta ne utice na pretragu, verifikaciju i
/// obracun - a to se rucnim testom gotovo nikad ne primijeti.
/// </summary>
public class PonistavanjeKesaTests
{
    [Fact]
    public void Ponistavanje_pravila_brise_isti_kljuc_koji_dozvole_citaju()
    {
        using var kes = new MemoryCache(new MemoryCacheOptions());
        kes.Set(KesPravilaKategorija.Kljuc, new object());

        KesPravilaKategorija.Ponisti(kes);

        Assert.False(kes.TryGetValue(KesPravilaKategorija.Kljuc, out _));
    }

    [Fact]
    public void Izmjena_tarife_na_istom_modelu_brise_samo_taj_model()
    {
        var kljucevi = CjenovnikService.KljuceviZaPonistavanje(4, 4);

        Assert.Single(kljucevi);
    }

    [Fact]
    public void Prebacivanje_tarife_na_drugi_model_brise_i_stari_model()
    {
        var kljucevi = CjenovnikService.KljuceviZaPonistavanje(7, 4);

        Assert.Equal(2, kljucevi.Count);
        Assert.Equal(kljucevi[0].Replace("7", "4"), kljucevi[1]);
    }

    [Fact]
    public void Dodavanje_i_brisanje_nemaju_prethodni_model()
    {
        var kljucevi = CjenovnikService.KljuceviZaPonistavanje(7, null);

        Assert.Single(kljucevi);
    }
}
