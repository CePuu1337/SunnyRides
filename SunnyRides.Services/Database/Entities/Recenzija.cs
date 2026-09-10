namespace SunnyRides.Services.Database.Entities;

/// <summary>Ocjena i komentar nakon zavrsenog najma. Skrivena recenzija ne ulazi u prosjecnu ocjenu ni u preporuke.</summary>
public class Recenzija
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public int VoziloId { get; set; }
    public int RezervacijaId { get; set; }
    public int Ocjena { get; set; }
    public string? Komentar { get; set; }
    public DateTime DatumKreiranja { get; set; }
    public bool Skrivena { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public Vozilo Vozilo { get; set; } = null!;
    public Rezervacija Rezervacija { get; set; } = null!;
}
