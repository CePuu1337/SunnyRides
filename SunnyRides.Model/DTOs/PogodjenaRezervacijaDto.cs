using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Rezervacija koju bi planirana blokada pogodila.
///
/// Uposlenik ovo vidi prije nego blokadu potvrdi, da zna koga treba nazvati. Zato su
/// tu i kontakt podaci klijenta, a ne samo brojevi.
/// </summary>
public class PogodjenaRezervacijaDto
{
    public int Id { get; set; }
    public string Broj { get; set; } = null!;
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public StatusRezervacije Status { get; set; }
    public decimal UkupanIznos { get; set; }
    public bool IsPaid { get; set; }

    public int KorisnikId { get; set; }
    public string? KlijentImePrezime { get; set; }
    public string? KlijentEmail { get; set; }
    public string? KlijentTelefon { get; set; }
}
