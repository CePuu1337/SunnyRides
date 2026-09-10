using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Vozacka dozvola klijenta i status njene verifikacije. Rok vazenja se provjerava na datum preuzimanja.</summary>
public class VozackaDozvola
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public string BrojDozvole { get; set; } = null!;
    public DateTime DatumIzdavanja { get; set; }
    public DateTime DatumIsteka { get; set; }
    public string? PutanjaSlike { get; set; }
    public StatusDozvole Status { get; set; } = StatusDozvole.NaCekanju;
    public string? RazlogOdbijanja { get; set; }
    public int? VerifikovaoKorisnikId { get; set; }
    public DateTime? DatumVerifikacije { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public Korisnik? VerifikovaoKorisnik { get; set; }
    public ICollection<DozvolaKategorija> Kategorije { get; set; } = new List<DozvolaKategorija>();
}
