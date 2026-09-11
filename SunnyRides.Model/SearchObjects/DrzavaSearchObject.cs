namespace SunnyRides.Model.SearchObjects;

public class DrzavaSearchObject : BaseSearchObject
{
    /// <summary>Dio naziva drzave. Svaki list endpoint ima bar jedan parametar pretrage.</summary>
    public string? Naziv { get; set; }

    public string? Skracenica { get; set; }
}
