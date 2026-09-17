using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Ono sto mobilna aplikacija treba da otvori Stripe PaymentSheet.
///
/// Iznos je tu samo za prikaz. Stvarni iznos je vec upisan u PaymentIntent na
/// serveru, pa ga klijent ne moze promijeniti ni kad bi ovaj broj izmijenio.
/// </summary>
public class PlatniIntentDto
{
    public int PlacanjeId { get; set; }
    public int RezervacijaId { get; set; }
    public string? RezervacijaBroj { get; set; }

    /// <summary>
    /// Tajna kojom PaymentSheet potvrdjuje bas ovaj intent. Prazna kad je placanje
    /// vec zavrseno, jer tada nema sta potvrditi.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Javni kljuc dolazi sa servera, iz .env fajla, da ga aplikacija ne mora imati
    /// upisanog u kodu.
    /// </summary>
    public string? PublishableKey { get; set; }

    public decimal Iznos { get; set; }
    public string Valuta { get; set; } = null!;
    public StatusPlacanja Status { get; set; }
    public bool IsPaid { get; set; }

    /// <summary>Koliko sekundi jos traje drzanje vozila - za odbrojavanje na ekranu placanja.</summary>
    public int? PreostaloSekundiDrzanja { get; set; }
}
