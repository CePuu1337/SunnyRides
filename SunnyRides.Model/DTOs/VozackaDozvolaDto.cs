using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

public class VozackaDozvolaDto
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public string BrojDozvole { get; set; } = null!;
    public DateTime DatumIzdavanja { get; set; }
    public DateTime DatumIsteka { get; set; }

    public StatusDozvole Status { get; set; }
    public string? RazlogOdbijanja { get; set; }
    public DateTime? DatumVerifikacije { get; set; }
    public string? VerifikovaoKorisnikIme { get; set; }
    public DateTime DatumKreiranja { get; set; }

    /// <summary>
    /// Fotografija dozvole je osjetljiv sadrzaj, pa se putanja nikad ne salje klijentu.
    /// Ovdje stoji samo je li prilozena; preuzima se kroz endpoint koji provjerava
    /// vlasnistvo nad resursom.
    /// </summary>
    public bool ImaPrednjuStranu { get; set; }

    /// <summary>Zadnja strana nosi kategorije, pa bez nje verifikacija nema sta provjeriti.</summary>
    public bool ImaZadnjuStranu { get; set; }

    /// <summary>Obje strane su prilozene. Dozvola se ne moze odobriti dok nisu.</summary>
    public bool ImaObjeStrane => ImaPrednjuStranu && ImaZadnjuStranu;

    /// <summary>Istekla je ako joj je rok prosao, bez obzira na status verifikacije.</summary>
    public bool Istekla { get; set; }

    public List<string> Kategorije { get; set; } = new();
    public List<int> KategorijaIds { get; set; } = new();

    public string? KlijentImePrezime { get; set; }
    public string? KlijentEmail { get; set; }
    public DateTime KlijentDatumRodjenja { get; set; }
}
