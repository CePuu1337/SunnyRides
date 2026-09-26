using SunnyRides.Services.Preporuke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Poredak "Preporuceno za vas" u pretrazi: najbolji primjerak svakog modela ide prije
/// ponovljenih primjeraka, a nijedno vozilo ne ispada iz liste.
/// </summary>
public class PoredakPoPreporuciTests
{
    [Fact]
    public void Prazna_lista_daje_praznu_listu()
    {
        Assert.Empty(PoredakPoPreporuci.Poredaj(Array.Empty<RangiranoVozilo>()));
    }

    [Fact]
    public void Razliciti_modeli_idu_po_skoru()
    {
        var poredak = PoredakPoPreporuci.Poredaj(new[]
        {
            new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 10, Skor: 0.2),
            new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 20, Skor: 0.9),
            new RangiranoVozilo(VoziloId: 3, ModelVozilaId: 30, Skor: 0.5)
        });

        Assert.Equal(new[] { 2, 3, 1 }, poredak);
    }

    [Fact]
    public void Tri_primjerka_istog_modela_ne_stoje_jedan_do_drugog()
    {
        // Model 10 ima najveci skor i tri primjerka. Ranije bi zauzeo prva tri mjesta.
        var poredak = PoredakPoPreporuci.Poredaj(new[]
        {
            new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 10, Skor: 0.9),
            new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 10, Skor: 0.9),
            new RangiranoVozilo(VoziloId: 3, ModelVozilaId: 10, Skor: 0.9),
            new RangiranoVozilo(VoziloId: 4, ModelVozilaId: 20, Skor: 0.7),
            new RangiranoVozilo(VoziloId: 5, ModelVozilaId: 30, Skor: 0.5)
        });

        // Prvi krug: po jedan od svakog modela. Zatim ostali primjerci modela 10.
        Assert.Equal(new[] { 1, 4, 5, 2, 3 }, poredak);
    }

    [Fact]
    public void Unutar_modela_prvi_je_primjerak_sa_vecim_skorom()
    {
        // Na rezervnom putu primjerci istog modela mogu imati razlicit skor, npr. zbog
        // grada u kojem korisnik obicno trazi.
        var poredak = PoredakPoPreporuci.Poredaj(new[]
        {
            new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 10, Skor: 0.4),
            new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 10, Skor: 0.8),
            new RangiranoVozilo(VoziloId: 3, ModelVozilaId: 20, Skor: 0.6)
        });

        Assert.Equal(new[] { 2, 3, 1 }, poredak);
    }

    [Fact]
    public void Jednak_skor_odlucuje_identifikator()
    {
        var ulaz = new[]
        {
            new RangiranoVozilo(VoziloId: 7, ModelVozilaId: 10, Skor: 0.5),
            new RangiranoVozilo(VoziloId: 3, ModelVozilaId: 20, Skor: 0.5),
            new RangiranoVozilo(VoziloId: 5, ModelVozilaId: 30, Skor: 0.5)
        };

        Assert.Equal(new[] { 3, 5, 7 }, PoredakPoPreporuci.Poredaj(ulaz));
        Assert.Equal(PoredakPoPreporuci.Poredaj(ulaz), PoredakPoPreporuci.Poredaj(ulaz.Reverse()));
    }

    [Fact]
    public void Nijedno_vozilo_ne_ispada()
    {
        var ulaz = Enumerable.Range(1, 12)
            .Select(i => new RangiranoVozilo(i, ModelVozilaId: i % 4, Skor: i / 12.0))
            .ToList();

        var poredak = PoredakPoPreporuci.Poredaj(ulaz);

        Assert.Equal(12, poredak.Count);
        Assert.Equal(ulaz.Select(x => x.VoziloId).OrderBy(x => x), poredak.OrderBy(x => x));
    }
}
