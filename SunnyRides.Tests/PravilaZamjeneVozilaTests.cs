using SunnyRides.Services.Rezervacije;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Zamjena vozila kod blokade: bilo koje drugo aktivno vozilo u istom gradu. Tip i
/// cijena nisu uslov; dozvolu i dostupnost provjeravaju njihovi servisi.
/// </summary>
public class PravilaZamjeneVozilaTests
{
    private static readonly VoziloZaZamjenu Rezervisano = new(Id: 1, Aktivno: true, GradId: 100);

    private static VoziloZaZamjenu Zamjensko(int id = 2, bool aktivno = true, int grad = 100) =>
        new(id, aktivno, grad);

    [Fact]
    public void Drugo_aktivno_vozilo_u_istom_gradu_je_dozvoljeno()
    {
        Assert.Null(PravilaZamjeneVozila.RazlogOdbijanja(Rezervisano, Zamjensko()));
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
