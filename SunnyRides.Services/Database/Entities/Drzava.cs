namespace SunnyRides.Services.Database.Entities;

public class Drzava
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public string Skracenica { get; set; } = null!;

    public ICollection<Grad> Gradovi { get; set; } = new List<Grad>();
}
