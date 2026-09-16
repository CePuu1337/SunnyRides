using SunnyRides.Model.Enums;

namespace SunnyRides.Model.SearchObjects;

public class VozackaDozvolaSearchObject : BaseSearchObject
{
    /// <summary>Dio imena, prezimena ili emaila klijenta.</summary>
    public string? Klijent { get; set; }

    public string? BrojDozvole { get; set; }

    public StatusDozvole? Status { get; set; }

    /// <summary>True vraca samo dozvole kojima je rok vazenja prosao.</summary>
    public bool? SamoIstekle { get; set; }
}
