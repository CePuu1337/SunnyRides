namespace SunnyRides.Services.Database.Entities;

/// <summary>Javna objava agencije koju vide svi klijenti u mobilnoj aplikaciji.</summary>
public class Obavijest
{
    public int Id { get; set; }
    public string Naslov { get; set; } = null!;
    public string Tekst { get; set; } = null!;
    public string? PutanjaSlike { get; set; }
    public DateTime DatumObjave { get; set; }
    public bool Aktivna { get; set; } = true;
}
