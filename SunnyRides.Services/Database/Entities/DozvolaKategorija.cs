namespace SunnyRides.Services.Database.Entities;

public class DozvolaKategorija
{
    public int Id { get; set; }
    public int VozackaDozvolaId { get; set; }
    public int KategorijaDozvoleId { get; set; }

    public VozackaDozvola VozackaDozvola { get; set; } = null!;
    public KategorijaDozvole KategorijaDozvole { get; set; } = null!;
}
