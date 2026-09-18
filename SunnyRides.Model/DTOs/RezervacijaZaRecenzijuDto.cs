namespace SunnyRides.Model.DTOs;

/// <summary>Zavrsen najam koji jos nije ocijenjen.</summary>
public class RezervacijaZaRecenzijuDto
{
    public int RezervacijaId { get; set; }
    public string Broj { get; set; } = null!;

    public int VoziloId { get; set; }
    public string? VoziloOpis { get; set; }
    public string? ThumbnailUrl { get; set; }

    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
}
