namespace SunnyRides.Services.Database.Entities;

/// <summary>Konkretan model vozila. Potrebna kategorija dozvole je svojstvo modela, ne pojedinacnog primjerka.</summary>
public class ModelVozila
{
    public int Id { get; set; }
    public int MarkaId { get; set; }
    public int TipVozilaId { get; set; }
    public int TipGorivaId { get; set; }
    public int KategorijaDozvoleId { get; set; }
    public string Naziv { get; set; } = null!;
    public int Kubikaza { get; set; }
    public decimal SnagaKw { get; set; }

    public Marka Marka { get; set; } = null!;
    public TipVozila TipVozila { get; set; } = null!;
    public TipGoriva TipGoriva { get; set; } = null!;
    public KategorijaDozvole KategorijaDozvole { get; set; } = null!;
    public ICollection<Vozilo> Vozila { get; set; } = new List<Vozilo>();
    public ICollection<Cjenovnik> Cjenovnici { get; set; } = new List<Cjenovnik>();
}
