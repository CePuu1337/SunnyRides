using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Sistemska poruka upucena jednom korisniku. Korisnik vidi iskljucivo svoje.</summary>
public class Notifikacija
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public int? RezervacijaId { get; set; }
    public string Naslov { get; set; } = null!;
    public string Tekst { get; set; } = null!;
    public TipNotifikacije Tip { get; set; }
    public bool Procitana { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public Rezervacija? Rezervacija { get; set; }
}
