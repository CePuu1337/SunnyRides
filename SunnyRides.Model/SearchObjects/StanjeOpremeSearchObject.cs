namespace SunnyRides.Model.SearchObjects;

public class StanjeOpremeSearchObject : BaseSearchObject
{
    public int? PoslovnicaId { get; set; }

    public int? VrstaOpremeId { get; set; }

    /// <summary>Dio naziva opreme.</summary>
    public string? Naziv { get; set; }
}
