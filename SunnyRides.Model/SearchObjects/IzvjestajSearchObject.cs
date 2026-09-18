namespace SunnyRides.Model.SearchObjects;

/// <summary>
/// Parametri izvjestaja: period i poslovnica.
///
/// Nije izveden iz <c>BaseSearchObject</c>: izvjestaj nije stranica zapisa nego zbirni
/// pregled, i dijeljenje po stranicama bi mu oduzelo smisao - zbirni red bi se odnosio
/// na stranicu umjesto na period. Broj redova ogranicava velicina flote.
/// </summary>
public class IzvjestajSearchObject
{
    public DateTime? Od { get; set; }
    public DateTime? Do { get; set; }

    /// <summary>Prazno znaci sve poslovnice.</summary>
    public int? PoslovnicaId { get; set; }
}
