namespace SunnyRides.Services.Database.Entities;

/// <summary>
/// Fotografija stanja vozila pri izdavanju ili povratu. Cuva se u privatnom folderu,
/// a Putanja je kljuc do fajla, ne adresa. Preuzimanje provjerava vlasnistvo jer
/// fotografije stete znaju biti predmet spora.
/// </summary>
public class FotografijaPrimopredaje
{
    public int Id { get; set; }
    public int PrimopredajaId { get; set; }
    public string Putanja { get; set; } = null!;

    public Primopredaja Primopredaja { get; set; } = null!;
}
