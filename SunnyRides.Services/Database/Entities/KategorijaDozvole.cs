namespace SunnyRides.Services.Database.Entities;

public class KategorijaDozvole
{
    public int Id { get; set; }
    public string Oznaka { get; set; } = null!;
    public string? Opis { get; set; }

    public ICollection<ModelVozila> Modeli { get; set; } = new List<ModelVozila>();
    public ICollection<PravilaKategorije> PravilaKategorija { get; set; } = new List<PravilaKategorije>();
    public ICollection<DozvolaKategorija> DozvoleKategorije { get; set; } = new List<DozvolaKategorija>();
}
