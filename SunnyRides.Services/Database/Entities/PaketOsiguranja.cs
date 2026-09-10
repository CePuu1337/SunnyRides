namespace SunnyRides.Services.Database.Entities;

public class PaketOsiguranja
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public decimal CijenaPoDanu { get; set; }
    public decimal IznosUcesca { get; set; }

    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
}
