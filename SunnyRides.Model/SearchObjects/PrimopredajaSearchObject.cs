using SunnyRides.Model.Enums;

namespace SunnyRides.Model.SearchObjects;

public class PrimopredajaSearchObject : BaseSearchObject
{
    public int? RezervacijaId { get; set; }
    public string? RezervacijaBroj { get; set; }
    public TipPrimopredaje? Tip { get; set; }

    /// <summary>Samo povrati na kojima je evidentirana steta.</summary>
    public bool? SaOstecenjem { get; set; }

    public DateTime? DatumOd { get; set; }
    public DateTime? DatumDo { get; set; }
}
