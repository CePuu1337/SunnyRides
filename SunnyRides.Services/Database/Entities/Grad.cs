namespace SunnyRides.Services.Database.Entities;

public class Grad
{
    public int Id { get; set; }
    public int DrzavaId { get; set; }
    public string Naziv { get; set; } = null!;
    public string? PostanskiBroj { get; set; }

    public Drzava Drzava { get; set; } = null!;
    public ICollection<Poslovnica> Poslovnice { get; set; } = new List<Poslovnica>();
}
