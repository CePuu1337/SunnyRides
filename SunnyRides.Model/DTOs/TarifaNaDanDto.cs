namespace SunnyRides.Model.DTOs;

/// <summary>
/// Cijena koju klijent placa za vozilo na dati dan: tarifa iz vazeceg cjenovnika (ili
/// sa samog vozila) puta sezonski mnozilac, i popusti koji ce se stvarno primijeniti.
///
/// Racuna je <c>PricingService</c>, isto mjesto koje racuna i rezervaciju, pa ono sto
/// aplikacija prikaze ne moze biti drugacije od onoga sto ce rezervacija naplatiti.
/// </summary>
public class TarifaNaDanDto
{
    public int VoziloId { get; set; }

    /// <summary>Dan za koji vazi - datum preuzimanja, ili danas kad termin nije zadan.</summary>
    public DateTime NaDan { get; set; }

    public decimal SatnaCijena { get; set; }
    public decimal DnevnaCijena { get; set; }

    /// <summary>Naziv sezone iz cjenovnika, ili null kad vazi osnovna tarifa vozila.</summary>
    public string? NazivSezone { get; set; }

    public int PopustPrag1 { get; set; }
    public decimal PopustProcenat1 { get; set; }
    public int PopustPrag2 { get; set; }
    public decimal PopustProcenat2 { get; set; }
}
