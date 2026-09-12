namespace SunnyRides.Model.DTOs;

/// <summary>
/// Period u kojem vozilo nije dostupno za najam - servis, kvar ili drugi razlog.
/// Blokada ulazi u istu provjeru dostupnosti kao i rezervacije.
/// </summary>
public class BlokadaVozilaDto
{
    public int Id { get; set; }
    public int VoziloId { get; set; }
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public string Razlog { get; set; } = null!;

    /// <summary>Ko je blokadu evidentirao. Cita se iz tokena, ne iz zahtjeva.</summary>
    public int KreiraoKorisnikId { get; set; }
    public string? KreiraoKorisnikIme { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public string? VoziloRegistarskaOznaka { get; set; }
    public string? ModelNaziv { get; set; }
    public string? PoslovnicaNaziv { get; set; }
}
