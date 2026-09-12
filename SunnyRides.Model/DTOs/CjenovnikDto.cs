namespace SunnyRides.Model.DTOs;

/// <summary>
/// Tarifa koja vazi za jedan model vozila u jednom sezonskom periodu.
///
/// Pragovi popusta su ovdje, a ne zakucani u kodu, jer ekran sa detaljima vozila
/// mora prikazati tacno one pragove koji se stvarno primjenjuju pri obracunu.
/// </summary>
public class CjenovnikDto
{
    public int Id { get; set; }
    public int ModelVozilaId { get; set; }
    public string Naziv { get; set; } = null!;
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }

    /// <summary>Sezonski mnozilac. 1,00 znaci bez uvecanja i bez umanjenja.</summary>
    public decimal Mnozilac { get; set; }

    /// <summary>Prazno znaci da se koristi tarifa upisana na samom vozilu.</summary>
    public decimal? SatnaTarifa { get; set; }
    public decimal? DnevnaTarifa { get; set; }

    public int PopustPrag1 { get; set; }
    public decimal PopustProcenat1 { get; set; }
    public int PopustPrag2 { get; set; }
    public decimal PopustProcenat2 { get; set; }

    public string? ModelNaziv { get; set; }
    public string? MarkaNaziv { get; set; }
}
