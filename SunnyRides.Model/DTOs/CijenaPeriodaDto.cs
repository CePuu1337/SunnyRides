namespace SunnyRides.Model.DTOs;

/// <summary>
/// Cijena najma za termin iz pretrage, uz vozilo u rezultatima.
///
/// Racuna je isti obracun kao i rezervaciju - trajanje sa tolerancijom od 59 minuta,
/// sezonski mnozilac i popust za duze najmove - samo bez opreme i osiguranja, koje
/// klijent bira tek u rezervaciji. Depozit stoji odvojeno jer se vraca.
/// </summary>
public class CijenaPeriodaDto
{
    /// <summary>Najam za cijeli period, sa sezonom i popustom. Bez depozita.</summary>
    public decimal IznosNajma { get; set; }

    public bool NaplataPoSatu { get; set; }
    public int BrojSati { get; set; }
    public int BrojDana { get; set; }

    public decimal ProcenatPopusta { get; set; }
    public decimal IznosDepozita { get; set; }
}
