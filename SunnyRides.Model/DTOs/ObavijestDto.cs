namespace SunnyRides.Model.DTOs;

/// <summary>Javna objava agencije. Klijent je vidi na pocetnom ekranu mobilne aplikacije.</summary>
public class ObavijestDto
{
    public int Id { get; set; }
    public string Naslov { get; set; } = null!;
    public string Tekst { get; set; } = null!;

    /// <summary>URL slike, nikad sadrzaj. Prazno kad obavijest nema sliku.</summary>
    public string? SlikaUrl { get; set; }

    /// <summary>Mala verzija iste slike, za listu.</summary>
    public string? ThumbnailUrl { get; set; }

    public DateTime DatumObjave { get; set; }
    public bool Aktivna { get; set; }
}
