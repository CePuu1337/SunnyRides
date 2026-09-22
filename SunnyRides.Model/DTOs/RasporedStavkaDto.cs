using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>Jedno zakazano preuzimanje ili vracanje u rasporedu za dan.</summary>
public class RasporedStavkaDto
{
    public int RezervacijaId { get; set; }
    public string Broj { get; set; } = null!;

    /// <summary>Izdavanje znaci da klijent dolazi po vozilo, Povrat da ga vraca.</summary>
    public TipPrimopredaje Akcija { get; set; }

    /// <summary>Vrijeme akcije o kojoj red govori - preuzimanja ili vracanja.</summary>
    public DateTime Vrijeme { get; set; }

    /// <summary>
    /// Cijeli ugovoreni termin. Treba formi za naknadni unos: vrijeme preuzimanja koje
    /// uposlenik upisuje mora pasti unutar termina, pa mu se granice i prikazuju.
    /// </summary>
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }

    public string? VoziloNaziv { get; set; }

    /// <summary>Vozilo na struju - forma primopredaje tada trazi napunjenost baterije.</summary>
    public bool JeElektricno { get; set; }
    public string? RegistarskaOznaka { get; set; }
    public string? KlijentImePrezime { get; set; }
    public string? PoslovnicaNaziv { get; set; }

    public StatusRezervacije StatusRezervacije { get; set; }

    /// <summary>True kad je primopredaja za ovu akciju vec evidentirana.</summary>
    public bool Obavljeno { get; set; }

    /// <summary>
    /// Je li izdavanje vozila evidentirano. Red za vracanje bez ovoga nema sta zaprimiti
    /// - aplikacija zato nudi izdavanje umjesto povrata, umjesto da pusti uposlenika u
    /// formu koju ce server odbiti.
    /// </summary>
    public bool IzdavanjeEvidentirano { get; set; }
}
