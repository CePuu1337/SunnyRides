namespace SunnyRides.Services.Database.Entities;

/// <summary>Jedna fotografija vozila. U bazu ide putanja do fajla, nikad base64 sadrzaj.</summary>
public class SlikaVozila
{
    public int Id { get; set; }
    public int VoziloId { get; set; }
    public string Putanja { get; set; } = null!;
    public string PutanjaThumbnail { get; set; } = null!;
    public int Redoslijed { get; set; }
    public bool JeGlavna { get; set; }

    public Vozilo Vozilo { get; set; } = null!;
}
