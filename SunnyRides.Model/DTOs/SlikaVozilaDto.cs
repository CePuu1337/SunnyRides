namespace SunnyRides.Model.DTOs;

public class SlikaVozilaDto
{
    public int Id { get; set; }
    public int VoziloId { get; set; }

    /// <summary>Putanja do pune slike. U bazi stoji putanja, nikad sadrzaj.</summary>
    public string Url { get; set; } = null!;

    public string ThumbnailUrl { get; set; } = null!;

    public int Redoslijed { get; set; }

    /// <summary>Slika koja predstavlja vozilo u listama. Tacno jedna po vozilu.</summary>
    public bool JeGlavna { get; set; }
}
