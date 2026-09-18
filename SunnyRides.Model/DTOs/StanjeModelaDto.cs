namespace SunnyRides.Model.DTOs;

/// <summary>
/// Stanje istreniranog modela preporuke.
///
/// Postoji da se u svakom trenutku moze odgovoriti na pitanje "na cemu je model ucen i
/// koliko grijesi", bez zavirivanja u log. RMSE je mjeren nad ocjenama koje model pri
/// ucenju nije vidio.
/// </summary>
public class StanjeModelaDto
{
    public bool Treniran { get; set; }

    public DateTime? TreniranUtc { get; set; }

    /// <summary>Ukupno redova u matrici korisnik x model vozila.</summary>
    public int BrojInterakcija { get; set; }

    /// <summary>Koliko od toga su stvarne ocjene korisnika.</summary>
    public int BrojStvarnihOcjena { get; set; }

    /// <summary>Koliko je procijenjeno iz zavrsenih najmova bez recenzije.</summary>
    public int BrojProcijenjenih { get; set; }

    public int BrojKorisnika { get; set; }

    public int BrojModelaVozila { get; set; }

    public int BrojIteracija { get; set; }

    /// <summary>Broj latentnih faktora, odabran pretragom nad skupom za odabir.</summary>
    public int Rang { get; set; }

    /// <summary>Jacina regularizacije, odabrana istom pretragom.</summary>
    public double Lambda { get; set; }

    /// <summary>
    /// Prosjecna greska najboljeg para parametara u unakrsnoj provjeri nad podacima za
    /// ucenje. Nije isto sto i <see cref="Rmse"/>, koji je mjeren na skupu koji model
    /// pri ucenju uopste nije vidio.
    /// </summary>
    public double? RmseOdabira { get; set; }

    /// <summary>
    /// Korijen srednje kvadratne greske, u zvjezdicama. Mjeren ugnijezdenom unakrsnom
    /// provjerom - svaka ocjena tacno jednom je bila u skupu za provjeru.
    /// </summary>
    public double? Rmse { get; set; }

    public double? Mae { get; set; }

    /// <summary>
    /// Koliko je model bolji od najprostijeg predvidjanja - uvijek isti prosjek. Nula
    /// znaci jednako dobar, pozitivno bolji, negativno losiji.
    /// </summary>
    public double? RKvadrat { get; set; }

    /// <summary>
    /// Greska najprostijeg predvidjanja - uvijek isti prosjek. Sluzi kao mjerilo:
    /// model je koristan tek ako je ispod ovog broja.
    /// </summary>
    public double? RmseOsnovni { get; set; }

    /// <summary>Broj ocjena nad kojima je greska izmjerena.</summary>
    public int BrojMjerenja { get; set; }

    public string Napomena { get; set; } = null!;
}
