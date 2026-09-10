namespace SunnyRides.Services.Database.Entities;

public class Poslovnica
{
    public int Id { get; set; }
    public int GradId { get; set; }
    public string Naziv { get; set; } = null!;
    public string Adresa { get; set; } = null!;
    public double? Latituda { get; set; }
    public double? Longituda { get; set; }
    public string? RadnoVrijeme { get; set; }

    public Grad Grad { get; set; } = null!;
    public ICollection<Vozilo> Vozila { get; set; } = new List<Vozilo>();
    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public ICollection<StanjeOpreme> StanjaOpreme { get; set; } = new List<StanjeOpreme>();
}
