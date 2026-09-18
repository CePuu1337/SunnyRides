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

/// <summary>Vozilo je vraceno i najam je zatvoren; slijedi obracun depozita klijentu.</summary>
public record PrimopredajaPoruka(int RezervacijaId, int PrimopredajaId);

/// <summary>
/// Kod za reset lozinke putuje kroz poruku, a ne kroz bazu: u bazi stoji samo njegov
/// hash, kao i kod lozinke. Kod u citljivom obliku postoji jedino u ovoj poruci i u
/// emailu koji klijent dobije.
/// </summary>
public record ResetLozinkePoruka(int KorisnikId, string Kod, DateTime IsticeUtc);

/// <summary>
/// Obavjestenje je upisano u bazu i treba ga gurnuti na uredjaj.
///
/// I ovdje putuje samo identifikator: API ce zapis procitati iz baze i poslati ga
/// kroz hub. KorisnikId je uz njega zato sto odredjuje grupu kojoj poruka ide, pa se
/// ne mora citati zapis da bi se znalo ima li uopste kome slati.
/// </summary>
public record NotifikacijaPoruka(int NotifikacijaId, int KorisnikId);
