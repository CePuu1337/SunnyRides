using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Povrat sredstava. Iznos se racuna iz Placanje.NaplaceniIznos, nikad iz cjenovnika.</summary>
public class Refund
{
    public int Id { get; set; }
    public int PlacanjeId { get; set; }
    public decimal Iznos { get; set; }
    public string Razlog { get; set; } = null!;
    public StatusPlacanja Status { get; set; } = StatusPlacanja.Created;
    public string? ProviderRefundId { get; set; }
    public int? KreiraoKorisnikId { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public Placanje Placanje { get; set; } = null!;
    public Korisnik? KreiraoKorisnik { get; set; }
}
