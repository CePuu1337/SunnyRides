namespace SunnyRides.Services.Database.Entities;

/// <summary>Katalog opreme koja se iznajmljuje uz vozilo. Cijena je ili po danu ili fiksna.</summary>
public class VrstaOpreme
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public decimal? CijenaPoDanu { get; set; }
    public decimal? FiksnaCijena { get; set; }

    public ICollection<StanjeOpreme> Stanja { get; set; } = new List<StanjeOpreme>();
    public ICollection<StavkaOpreme> Stavke { get; set; } = new List<StavkaOpreme>();
}
