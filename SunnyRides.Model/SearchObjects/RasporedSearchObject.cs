namespace SunnyRides.Model.SearchObjects;

/// <summary>
/// Raspored preuzimanja i vracanja. Aplikacija salje pocetak i kraj dana u UTC-u,
/// jer "danas" zavisi od vremenske zone poslovnice. Bez toga server uzima danasnji
/// dan po UTC-u.
/// </summary>
public class RasporedSearchObject : BaseSearchObject
{
    public DateTime? Od { get; set; }
    public DateTime? Do { get; set; }
    public int? PoslovnicaId { get; set; }
}
