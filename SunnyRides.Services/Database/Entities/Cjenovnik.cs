namespace SunnyRides.Services.Database.Entities;

/// <summary>Tarifa koja vazi za odredjeni model u odredjenom sezonskom periodu.</summary>
public class Cjenovnik
{
    public int Id { get; set; }
    public int ModelVozilaId { get; set; }
    public string Naziv { get; set; } = null!;
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public decimal Mnozilac { get; set; } = 1m;
    public decimal? SatnaTarifa { get; set; }
    public decimal? DnevnaTarifa { get; set; }
    public int PopustPrag1 { get; set; }
    public decimal PopustProcenat1 { get; set; }
    public int PopustPrag2 { get; set; }
    public decimal PopustProcenat2 { get; set; }

    public ModelVozila ModelVozila { get; set; } = null!;
}
