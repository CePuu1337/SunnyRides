namespace SunnyRides.Model.Poruke;

/// <summary>
/// Poruke nose samo identifikatore, a ne gotov tekst emaila.
///
/// Worker sve ostalo procita iz baze. Tako poruka ostaje mala i tacna i onda kad se
/// obradi minutu kasnije - iznos, status i podaci o klijentu su tada svjezi, a ne
/// onakvi kakvi su bili u trenutku slanja.
/// </summary>
public record RezervacijaPoruka(int RezervacijaId);

public record PlacanjePoruka(int RezervacijaId, int PlacanjeId);

public record PovratPoruka(int RezervacijaId, int PovratId);

public record DozvolaPoruka(int DozvolaId);

/// <summary>
/// Kod za reset lozinke putuje kroz poruku, a ne kroz bazu: u bazi stoji samo njegov
/// hash, kao i kod lozinke. Kod u citljivom obliku postoji jedino u ovoj poruci i u
/// emailu koji klijent dobije.
/// </summary>
public record ResetLozinkePoruka(int KorisnikId, string Kod, DateTime IsticeUtc);
