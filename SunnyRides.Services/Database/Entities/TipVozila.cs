namespace SunnyRides.Services.Database.Entities;

public class TipVozila
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;

    public ICollection<ModelVozila> Modeli { get; set; } = new List<ModelVozila>();
    public ICollection<PravilaKategorije> PravilaKategorija { get; set; } = new List<PravilaKategorije>();
}
