using SunnyRides.Model.Enums;
using SunnyRides.Services.Rezervacije;
using Xunit;

namespace SunnyRides.Tests;

/// <summary>
/// Rezervacija ima tacno cetiri statusa i tacno cetiri dozvoljena prelaza. Sve ostalo
/// mora biti odbijeno - ukljucujuci prelaz u isti status i ozivljavanje terminalnog.
/// </summary>
public class PrelaziRezervacijeTests
{
    [Theory]
    [InlineData(StatusRezervacije.Pending, StatusRezervacije.Confirmed)]
    [InlineData(StatusRezervacije.Pending, StatusRezervacije.Cancelled)]
    [InlineData(StatusRezervacije.Confirmed, StatusRezervacije.Cancelled)]
    [InlineData(StatusRezervacije.Confirmed, StatusRezervacije.Completed)]
    public void DozvoljeniPrelaziProlaze(StatusRezervacije iz, StatusRezervacije u)
    {
        Assert.True(PrelaziRezervacije.JeDozvoljen(iz, u));
    }

    [Theory]
    [InlineData(StatusRezervacije.Pending, StatusRezervacije.Completed)]     // bez placanja nema zavrsetka
    [InlineData(StatusRezervacije.Confirmed, StatusRezervacije.Pending)]     // nazad u cekanje se ne ide
    [InlineData(StatusRezervacije.Cancelled, StatusRezervacije.Confirmed)]   // otkazana se ne ozivljava
    [InlineData(StatusRezervacije.Cancelled, StatusRezervacije.Pending)]
    [InlineData(StatusRezervacije.Completed, StatusRezervacije.Cancelled)]   // zavrsena se ne otkazuje
    [InlineData(StatusRezervacije.Completed, StatusRezervacije.Confirmed)]
    public void ZabranjeniPrelaziPadaju(StatusRezervacije iz, StatusRezervacije u)
    {
        Assert.False(PrelaziRezervacije.JeDozvoljen(iz, u));
    }

    [Theory]
    [InlineData(StatusRezervacije.Pending)]
    [InlineData(StatusRezervacije.Confirmed)]
    [InlineData(StatusRezervacije.Cancelled)]
    [InlineData(StatusRezervacije.Completed)]
    public void PrelazUIstiStatusNijeDozvoljen(StatusRezervacije status)
    {
        Assert.False(PrelaziRezervacije.JeDozvoljen(status, status));
    }

    [Theory]
    [InlineData(StatusRezervacije.Cancelled)]
    [InlineData(StatusRezervacije.Completed)]
    public void TerminalniStatusiNemajuIzlaz(StatusRezervacije status)
    {
        Assert.True(PrelaziRezervacije.JeTerminalan(status));
        Assert.Empty(PrelaziRezervacije.Iz(status));
    }

    [Theory]
    [InlineData(StatusRezervacije.Pending)]
    [InlineData(StatusRezervacije.Confirmed)]
    public void AktivniStatusiImajuTacnoDvaIzlaza(StatusRezervacije status)
    {
        Assert.Equal(2, PrelaziRezervacije.Iz(status).Count);
    }

    [Fact]
    public void IzdavanjeVozilaNeMijenjaStatus()
    {
        // Rezervacija ostaje Confirmed i dok je vozilo fizicki kod klijenta.
        // Fizicki tok se prati kroz zapise Primopredaja, jer uputstvo propisuje
        // tacno cetiri statusa - peti se ne smije dodati.
        var izlazi = PrelaziRezervacije.Iz(StatusRezervacije.Confirmed);

        Assert.Contains(StatusRezervacije.Completed, izlazi);
        Assert.Contains(StatusRezervacije.Cancelled, izlazi);
        Assert.Equal(2, izlazi.Count);
    }

    [Fact]
    public void PorukaZaTerminalanStatusKazeDaSeVisaNeMijenja()
    {
        var poruka = PrelaziRezervacije.PorukaOdbijanja(
            StatusRezervacije.Completed, StatusRezervacije.Cancelled);

        Assert.Contains("zavrsena", poruka);
        Assert.Contains("ne mijenja", poruka);
    }

    [Fact]
    public void PorukaZaNedozvoljenPrelazNabrajaStaJeMoguce()
    {
        var poruka = PrelaziRezervacije.PorukaOdbijanja(
            StatusRezervacije.Pending, StatusRezervacije.Completed);

        Assert.Contains("na cekanju", poruka);
        Assert.Contains("potvrdjena", poruka);
        Assert.Contains("otkazana", poruka);
    }
}
