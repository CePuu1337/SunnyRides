using SunnyRides.Model.Enums;

namespace SunnyRides.Model.SearchObjects;

/// <summary>
/// Pretraga vlastitih obavjestenja.
///
/// Nema polja za korisnika. Vlasnik se cita iz tokena, pa ga nema smisla slati - a
/// da ga ima, bilo bi dovoljno upisati tudji broj i procitati tudja obavjestenja.
/// </summary>
public class NotifikacijaSearchObject : BaseSearchObject
{
    /// <summary>Tacka razdvajanja procitanih i neprocitanih. Bez vrijednosti vraca oboje.</summary>
    public bool? Procitana { get; set; }

    public TipNotifikacije? Tip { get; set; }

    public int? RezervacijaId { get; set; }

    /// <summary>Trazi se i po naslovu i po tekstu, jer korisnik pamti jedno ili drugo.</summary>
    public string? Tekst { get; set; }

    public DateTime? OdDatuma { get; set; }

    public DateTime? DoDatuma { get; set; }
}
