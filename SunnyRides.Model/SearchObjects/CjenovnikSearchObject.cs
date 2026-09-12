namespace SunnyRides.Model.SearchObjects;

public class CjenovnikSearchObject : BaseSearchObject
{
    public string? Naziv { get; set; }

    public int? ModelVozilaId { get; set; }

    /// <summary>Tarife koje vaze na zadati dan.</summary>
    public DateTime? VaziNaDatum { get; set; }
}
