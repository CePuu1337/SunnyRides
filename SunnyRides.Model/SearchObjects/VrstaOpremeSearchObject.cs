namespace SunnyRides.Model.SearchObjects;

public class VrstaOpremeSearchObject : BaseSearchObject
{
    public string? Naziv { get; set; }

    /// <summary>Kad je postavljeno, vraca samo opremu koja se naplacuje po danu.</summary>
    public bool? SamoPoDanu { get; set; }
}
