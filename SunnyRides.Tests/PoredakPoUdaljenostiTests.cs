using SunnyRides.Services.Lokacija;
using SunnyRides.Services.Preporuke;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Poredak "Najblize meni": poslovnice od najblize, a unutar poslovnice poredak po
/// preporuci.
/// </summary>
public class PoredakPoUdaljenostiTests
{
    // Poslovnica 1 je 120 km daleko, poslovnica 2 svega 5 km.
    private static readonly Dictionary<int, double> Udaljenosti = new() { [1] = 120, [2] = 5 };

    [Fact]
    public void Prazna_lista_daje_praznu_listu()
    {
        var poredak = PoredakPoUdaljenosti.Poredaj(
            Array.Empty<RangiranoVozilo>(), new Dictionary<int, int>(), Udaljenosti);

        Assert.Empty(poredak);
    }

    [Fact]
    public void Blize_poslovnice_idu_prve_i_kad_daleka_ima_bolji_skor()
    {
        var poredak = PoredakPoUdaljenosti.Poredaj(
            new[]
            {
                new RangiranoVozilo(VoziloId: 10, ModelVozilaId: 100, Skor: 0.95),
                new RangiranoVozilo(VoziloId: 20, ModelVozilaId: 200, Skor: 0.30)
            },
            new Dictionary<int, int> { [10] = 1, [20] = 2 },
            Udaljenosti);

        Assert.Equal(new[] { 20, 10 }, poredak);
    }

    [Fact]
    public void Unutar_poslovnice_vazi_poredak_po_preporuci()
    {
        var poredak = PoredakPoUdaljenosti.Poredaj(
            new[]
            {
                new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 100, Skor: 0.9),
                new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 100, Skor: 0.9),
                new RangiranoVozilo(VoziloId: 3, ModelVozilaId: 200, Skor: 0.5),
                new RangiranoVozilo(VoziloId: 4, ModelVozilaId: 300, Skor: 0.7)
            },
            new Dictionary<int, int> { [1] = 2, [2] = 2, [3] = 2, [4] = 1 },
            Udaljenosti);

        // U bliskoj poslovnici: po jedan primjerak svakog modela po skoru (1, 3), pa
        // ponovljeni primjerak (2). Tek onda daleka poslovnica (4).
        Assert.Equal(new[] { 1, 3, 2, 4 }, poredak);
    }

    [Fact]
    public void Poslovnica_bez_koordinata_ide_na_kraj()
    {
        var poredak = PoredakPoUdaljenosti.Poredaj(
            new[]
            {
                new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 100, Skor: 0.9),
                new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 200, Skor: 0.1)
            },
            new Dictionary<int, int> { [1] = 99, [2] = 1 },
            Udaljenosti);

        Assert.Equal(new[] { 2, 1 }, poredak);
    }

    [Fact]
    public void Poslovnice_na_istoj_udaljenosti_imaju_stalan_redoslijed()
    {
        var jednako = new Dictionary<int, double> { [7] = 30, [3] = 30 };
        var vozila = new[]
        {
            new RangiranoVozilo(VoziloId: 1, ModelVozilaId: 100, Skor: 0.9),
            new RangiranoVozilo(VoziloId: 2, ModelVozilaId: 200, Skor: 0.1)
        };
        var poslovnice = new Dictionary<int, int> { [1] = 7, [2] = 3 };

        var prvi = PoredakPoUdaljenosti.Poredaj(vozila, poslovnice, jednako);
        var drugi = PoredakPoUdaljenosti.Poredaj(vozila.AsEnumerable().Reverse(), poslovnice, jednako);

        Assert.Equal(new[] { 2, 1 }, prvi);
        Assert.Equal(prvi, drugi);
    }
}
