using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Klijent u padajucoj listi pri rucnom unosu rezervacije.
///
/// Namjerno uzi od <see cref="KorisnikDto"/>: uposlenik pri unosu treba prepoznati
/// klijenta i znati hoce li mu rezervacija uopste proci, a ne vidjeti cijeli nalog.
/// Nema uloga, datuma rodjenja ni slike - to uposleniku za ovaj posao ne treba.
/// </summary>
public class KlijentZaOdabirDto
{
    public int Id { get; set; }
    public string Ime { get; set; } = null!;
    public string Prezime { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefon { get; set; }

    /// <summary>Blokiran klijent ne moze rezervisati. Prikazuje se da uposlenik ne pokusava uzalud.</summary>
    public bool Blokiran { get; set; }

    /// <summary>
    /// Stanje vozacke dozvole, prazno ako je klijent nije ni predao. Server rezervaciju
    /// bez odobrene dozvole ionako odbija; ovdje je da uposlenik to vidi unaprijed.
    /// </summary>
    public StatusDozvole? StatusDozvole { get; set; }
}
