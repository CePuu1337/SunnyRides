using SunnyRides.Model.Enums;

namespace SunnyRides.Model.SearchObjects;

public class PlacanjeSearchObject : BaseSearchObject
{
    public int? RezervacijaId { get; set; }

    /// <summary>Dio broja rezervacije, npr. "SR-2026-01".</summary>
    public string? RezervacijaBroj { get; set; }

    public StatusPlacanja? Status { get; set; }

    /// <summary>
    /// True vraca samo placanja koja imaju povrat odbijen kod Stripe-a - osoblje
    /// ovim nalazi ono sto treba pokusati ponovo.
    /// </summary>
    public bool? SamoNeuspjeliPovrati { get; set; }
}
