namespace SunnyRides.Services.Database.Entities;

public class Marka
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;

    public ICollection<ModelVozila> Modeli { get; set; } = new List<ModelVozila>();
}
