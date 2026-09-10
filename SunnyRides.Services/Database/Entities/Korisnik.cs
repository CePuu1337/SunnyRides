namespace SunnyRides.Services.Database.Entities;

public class Korisnik
{
    public int Id { get; set; }
    public string KorisnickoIme { get; set; } = null!;
    public string Ime { get; set; } = null!;
    public string Prezime { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefon { get; set; }
    public DateTime DatumRodjenja { get; set; }
    public string LozinkaHash { get; set; } = null!;
    public string? PutanjaSlike { get; set; }
    public bool Aktivan { get; set; } = true;
    public bool Blokiran { get; set; }
    public DateTime DatumRegistracije { get; set; }

    public ICollection<KorisnikRole> KorisnikRole { get; set; } = new List<KorisnikRole>();
    public VozackaDozvola? VozackaDozvola { get; set; }
    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public ICollection<Recenzija> Recenzije { get; set; } = new List<Recenzija>();
    public ICollection<Notifikacija> Notifikacije { get; set; } = new List<Notifikacija>();
    public ICollection<HistorijaPretrage> HistorijaPretraga { get; set; } = new List<HistorijaPretrage>();
}
