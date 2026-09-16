using SunnyRides.Services.Dozvole;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Hijerarhija kategorija nije napisana u kodu nego izlazi iz pravila u sifrarniku.
/// Ovi testovi to i provjeravaju: mijenja se samo tabela pravila, nikad grana u kodu.
/// </summary>
public class PravilaPokrivenostiTests
{
    private const int Skuter = 1;
    private const int Motocikl = 2;
    private const int Quad = 3;

    private const int A1 = 10;
    private const int A = 20;
    private const int B = 30;

    /// <summary>Ista pravila kakva stoje u seed sifrarniku.</summary>
    private static readonly List<PraviloUlaz> Pravila = new()
    {
        new PraviloUlaz(A1, Skuter,   MaxKubikaza: 125,  MaxSnagaKw: 11m,  MinGodine: 16),
        new PraviloUlaz(A1, Motocikl, MaxKubikaza: 125,  MaxSnagaKw: 11m,  MinGodine: 16),
        new PraviloUlaz(A,  Skuter,   MaxKubikaza: null, MaxSnagaKw: null, MinGodine: 24),
        new PraviloUlaz(A,  Motocikl, MaxKubikaza: null, MaxSnagaKw: null, MinGodine: 24),
        new PraviloUlaz(B,  Quad,     MaxKubikaza: null, MaxSnagaKw: null, MinGodine: 18)
    };

    [Fact]
    public void KategorijaAPokrivaISvojuIA1()
    {
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { A }, godine: 30);

        Assert.Contains(A, pokrivene);
        Assert.Contains(A1, pokrivene);
        Assert.DoesNotContain(B, pokrivene);
    }

    [Fact]
    public void KategorijaA1NePokrivaA()
    {
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { A1 }, godine: 30);

        Assert.Contains(A1, pokrivene);
        Assert.DoesNotContain(A, pokrivene);
        Assert.DoesNotContain(B, pokrivene);
    }

    [Fact]
    public void KategorijaBJeOdvojena()
    {
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { B }, godine: 30);

        Assert.Equal(new[] { B }, pokrivene);
    }

    [Fact]
    public void ViseKategorijaSeSabira()
    {
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { A1, B }, godine: 30);

        Assert.Contains(A1, pokrivene);
        Assert.Contains(B, pokrivene);
        Assert.DoesNotContain(A, pokrivene);
    }

    [Fact]
    public void MladjiOdPropisaneDobiNeDobijaPravaKategorije()
    {
        // Pravilo za A trazi 24 godine; sa 20 ta kategorija jos ne vrijedi,
        // pa ne pokriva ni A1.
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { A }, godine: 20);

        Assert.Empty(pokrivene);
    }

    [Fact]
    public void SaTacnoPropisanomDobiKategorijaVrijedi()
    {
        var pokrivene = PravilaPokrivenosti.Pokrivene(Pravila, new[] { A }, godine: 24);

        Assert.Contains(A, pokrivene);
    }

    [Fact]
    public void PromjenaPropisaMijenjaHijerarhijuBezIzmjeneKoda()
    {
        // Ako A1 dobije granicu od 125 cm3 a A ogranicenje na 500 cm3, A i dalje
        // pokriva A1 - jer je 500 vece od 125.
        var noviPropis = new List<PraviloUlaz>
        {
            new(A1, Motocikl, 125, 11m, 16),
            new(A,  Motocikl, 500, 47m, 24)
        };

        var pokrivene = PravilaPokrivenosti.Pokrivene(noviPropis, new[] { A }, godine: 30);

        Assert.Contains(A1, pokrivene);
    }

    [Fact]
    public void StrozijaKategorijaNePokrivaBlazuNiUzGranice()
    {
        var noviPropis = new List<PraviloUlaz>
        {
            new(A1, Motocikl, 125, 11m, 16),
            new(A,  Motocikl, 500, 47m, 24)
        };

        var pokrivene = PravilaPokrivenosti.Pokrivene(noviPropis, new[] { A1 }, godine: 30);

        Assert.DoesNotContain(A, pokrivene);
    }

    [Fact]
    public void BezIjedneKategorijeNemaNistaDozvoljeno()
    {
        Assert.Empty(PravilaPokrivenosti.Pokrivene(Pravila, Array.Empty<int>(), godine: 40));
    }
}
