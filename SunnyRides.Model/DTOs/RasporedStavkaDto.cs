using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>Jedno zakazano preuzimanje ili vracanje u rasporedu za dan.</summary>
public class RasporedStavkaDto
{
    public int RezervacijaId { get; set; }
    public string Broj { get; set; } = null!;

    /// <summary>Izdavanje znaci da klijent dolazi po vozilo, Povrat da ga vraca.</summary>
    public TipPrimopredaje Akcija { get; set; }

    public DateTime Vrijeme { get; set; }

    public string? VoziloNaziv { get; set; }
    public string? RegistarskaOznaka { get; set; }
    public string? KlijentImePrezime { get; set; }
    public string? PoslovnicaNaziv { get; set; }

    public StatusRezervacije StatusRezervacije { get; set; }

    /// <summary>True kad je primopredaja za ovu akciju vec evidentirana.</summary>
    public bool Obavljeno { get; set; }
}
