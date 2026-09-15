namespace SunnyRides.Model.DTOs;

/// <summary>
/// Razrada cijene najma, stavku po stavku.
///
/// Klijentu se ne salje samo konacan iznos nego i kako je do njega doslo - koliko
/// dana, po kojoj tarifi, sa kojim sezonskim mnoziocem i kojim popustom. Uputstvo
/// trazi da forma za rezervaciju prikaze svaku stavku pojedinacno, a i sam obracun
/// je time provjerljiv: ako se broj ne poklapa, vidi se na kojoj liniji.
/// </summary>
public class CijenaRezervacijeDto
{
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }

    /// <summary>True kad je najam kraci od sest sati i naplacuje se po satu.</summary>
    public bool NaplataPoSatu { get; set; }

    public int BrojSati { get; set; }
    public int BrojDana { get; set; }

    public decimal SatnaTarifa { get; set; }
    public decimal DnevnaTarifa { get; set; }

    /// <summary>Sezonski mnozilac. 1,00 znaci da za taj datum nema definisane sezone.</summary>
    public decimal Mnozilac { get; set; }
    public string? NazivSezone { get; set; }

    /// <summary>Tarifa puta trajanje, prije sezonskog mnozioca.</summary>
    public decimal OsnovicaNajma { get; set; }

    /// <summary>Osnovica poslije mnozioca i poslije popusta - ono sto najam stvarno kosta.</summary>
    public decimal IznosNajma { get; set; }

    public decimal ProcenatPopusta { get; set; }
    public decimal IznosPopusta { get; set; }

    public List<StavkaCijeneDto> Oprema { get; set; } = new();
    public decimal IznosOpreme { get; set; }

    public int? PaketOsiguranjaId { get; set; }
    public string? PaketOsiguranjaNaziv { get; set; }
    public decimal IznosOsiguranja { get; set; }

    /// <summary>Depozit se naplacuje unaprijed i vraca pri urednom povratu vozila.</summary>
    public decimal IznosDepozita { get; set; }

    /// <summary>Najam, oprema, osiguranje i depozit zajedno - iznos koji se naplacuje.</summary>
    public decimal UkupanIznos { get; set; }
}
