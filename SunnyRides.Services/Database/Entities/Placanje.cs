using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Jedan pokusaj naplate kroz Stripe. NaplaceniIznos dolazi iz Stripe odgovora i osnova je za svaki povrat.</summary>
public class Placanje
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public decimal Iznos { get; set; }
    public string Valuta { get; set; } = "EUR";
    public StatusPlacanja Status { get; set; } = StatusPlacanja.Created;
    public string Provider { get; set; } = "Stripe";
    public string? ProviderPaymentIntentId { get; set; }
    public decimal? NaplaceniIznos { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime DatumKreiranja { get; set; }
    public DateTime? DatumAzuriranja { get; set; }

    public Rezervacija Rezervacija { get; set; } = null!;
    public ICollection<Refund> Refundi { get; set; } = new List<Refund>();
}
