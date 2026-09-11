namespace SunnyRides.Model.SearchObjects;

public class ModelVozilaSearchObject : BaseSearchObject
{
    public string? Naziv { get; set; }

    public int? MarkaId { get; set; }

    public int? TipVozilaId { get; set; }

    public int? TipGorivaId { get; set; }

    public int? KategorijaDozvoleId { get; set; }

    public int? KubikazaOd { get; set; }

    public int? KubikazaDo { get; set; }
}
