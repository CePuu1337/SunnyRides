namespace SunnyRides.Model.SearchObjects;

public class PoslovnicaSearchObject : BaseSearchObject
{
    public string? Naziv { get; set; }

    public string? Adresa { get; set; }

    public int? GradId { get; set; }

    /// <summary>Filtriranje po drzavi ide kroz grad, bez dodatne kolone na poslovnici.</summary>
    public int? DrzavaId { get; set; }
}
