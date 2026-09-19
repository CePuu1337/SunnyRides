namespace SunnyRides.Model.SearchObjects;

/// <summary>Pretraga klijenata pri rucnom unosu rezervacije.</summary>
public class KlijentSearchObject : BaseSearchObject
{
    /// <summary>Dio imena, prezimena, emaila ili broja telefona.</summary>
    public string? Tekst { get; set; }
}
