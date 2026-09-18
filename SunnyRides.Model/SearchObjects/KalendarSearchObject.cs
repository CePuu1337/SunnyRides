namespace SunnyRides.Model.SearchObjects;

/// <summary>
/// Okvir kalendara flote.
///
/// Nije izveden iz <c>BaseSearchObject</c> namjerno: odgovor nije stranica zapisa nego
/// pregled kroz vrijeme, a broj redova ogranicava period i filteri. Gornja granica
/// perioda je u servisu.
/// </summary>
public class KalendarSearchObject
{
    public DateTime? Od { get; set; }
    public DateTime? Do { get; set; }

    public int? PoslovnicaId { get; set; }
    public int? TipVozilaId { get; set; }

    /// <summary>Dio naziva modela ili registarske oznake, za brzo pronalazenje vozila.</summary>
    public string? Vozilo { get; set; }
}
