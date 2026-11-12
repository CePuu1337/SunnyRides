namespace SunnyRides.Model.SearchObjects;

public class PravilaKategorijeSearchObject : BaseSearchObject
{
    public int? KategorijaDozvoleId { get; set; }

    public int? TipVozilaId { get; set; }

    /// <summary>Dio oznake kategorije ili naziva tipa vozila, za pretragu u listi.</summary>
    public string? Tekst { get; set; }
}
