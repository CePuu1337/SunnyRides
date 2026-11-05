namespace SunnyRides.Model.DTOs;

/// <summary>
/// Jedan period politike otkazivanja za konkretan termin: ako klijent otkaze u ovom
/// periodu, dobija nazad ovoliko najma. Depozit se vraca uvijek i nije dio ovoga.
/// </summary>
public class PeriodPovrataDto
{
    /// <summary>Od kada vazi (UTC). Prazno znaci "od sada".</summary>
    public DateTime? Od { get; set; }

    /// <summary>Do kada vazi (UTC). Prazno znaci "do preuzimanja".</summary>
    public DateTime? Do { get; set; }

    public decimal ProcenatPovrataNajma { get; set; }
}
