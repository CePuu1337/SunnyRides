namespace SunnyRides.Model.DTOs;

public class KorisnikDto
{
    public int Id { get; set; }
    public string KorisnickoIme { get; set; } = null!;
    public string Ime { get; set; } = null!;
    public string Prezime { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefon { get; set; }
    public DateTime DatumRodjenja { get; set; }
    public DateTime DatumRegistracije { get; set; }

    /// <summary>URL profilne slike, nikad sadrzaj.</summary>
    public string? PutanjaSlike { get; set; }

    /// <summary>Mala verzija iste slike, za liste.</summary>
    public string? ThumbnailUrl { get; set; }
    public bool Aktivan { get; set; }
    public bool Blokiran { get; set; }

    /// <summary>Nazivi uloga, ne identifikatori - klijent ih prikazuje i po njima odlucuje sta nudi.</summary>
    public List<string> Uloge { get; set; } = new();
}
