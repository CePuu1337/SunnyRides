namespace SunnyRides.Services.Database.Entities;

/// <summary>Dokumentacija stanja vozila. Download provjerava vlasnistvo - fotografije stete mogu biti predmet spora.</summary>
public class FotografijaPrimopredaje
{
    public int Id { get; set; }
    public int PrimopredajaId { get; set; }
    public string Putanja { get; set; } = null!;
    public string PutanjaThumbnail { get; set; } = null!;

    public Primopredaja Primopredaja { get; set; } = null!;
}
