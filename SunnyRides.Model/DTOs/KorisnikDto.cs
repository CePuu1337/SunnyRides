namespace SunnyRides.Model.DTOs;

public class KorisnikDto
{
    public int Id { get; set; }
    public string KorisnickoIme { get; set; } = null!;
    public string Ime { get; set; } = null!;
    public string Prezime { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefon { get; set; }
    public string? PutanjaSlike { get; set; }
    public bool Aktivan { get; set; }
    public bool Blokiran { get; set; }

    /// <summary>Nazivi uloga, ne identifikatori - klijent ih prikazuje i po njima odlucuje sta nudi.</summary>
    public List<string> Uloge { get; set; } = new();
}
