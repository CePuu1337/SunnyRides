namespace SunnyRides.Model.SearchObjects;

public class GradSearchObject : BaseSearchObject
{
    /// <summary>Dio naziva grada. Svaki list endpoint ima bar jedan parametar pretrage.</summary>
    public string? Naziv { get; set; }

    public int? DrzavaId { get; set; }

    public string? PostanskiBroj { get; set; }
}
