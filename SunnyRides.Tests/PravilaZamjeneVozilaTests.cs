using SunnyRides.Services.Rezervacije;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Zamjena vozila kod blokade: klijent smije dobiti samo vozilo istog tipa, iste ili
/// vise klase, u istom gradu.
/// </summary>
public class PravilaZamjeneVozilaTests
{
    private static readonly VoziloZaZamjenu Rezervisano =
        new(Id: 1, Aktivno: true, TipVozilaId: 10, DnevnaTarifa: 40m, GradId: 100);

    private static VoziloZaZamjenu Zamjensko(
        int id = 2, bool aktivno = true, int tip = 10, decimal tarifa = 40m, int grad = 100) =>
        new(id, aktivno, tip, tarifa, grad);

    [Fact]
    public void Isti_tip_ista_tarifa_isti_grad_je_dozvoljeno()
    {
        Assert.Null(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko()));
    }

    [Fact]
    public void Skuplje_vozilo_istog_tipa_je_dozvoljeno()
    {
        Assert.Null(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(tarifa: 55m)));
    }

    [Fact]
    public void Jeftinije_vozilo_je_nize_klase()
    {
        var razlog = PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(tarifa: 39.99m));

        Assert.NotNull(razlog);
        Assert.Contains("nize klase", razlog);
    }

    [Fact]
    public void Drugi_tip_vozila_se_odbija()
    {
        Assert.NotNull(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(tip: 11)));
    }

    [Fact]
    public void Drugi_grad_se_odbija()
    {
        Assert.NotNull(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(grad: 200)));
    }

    [Fact]
    public void Neaktivno_vozilo_se_odbija()
    {
        Assert.NotNull(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(aktivno: false)));
    }

    [Fact]
    public void Isto_vozilo_nije_zamjena()
    {
        Assert.NotNull(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko(id: 1)));
    }
}
