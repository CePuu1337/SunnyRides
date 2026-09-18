using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Zauzetost flote kroz vrijeme - jedan red po vozilu.
///
/// Prazan prostor u ovom pregledu znaci da je vozilo stvarno slobodno: blokovi dolaze iz
/// istih zapisa koje i provjera dostupnosti smatra zauzecem, pa se ne moze desiti da
/// kalendar pokaze slobodan termin koji rezervacija odbije.
/// </summary>
public class KalendarFloteDto
{
    public DateTime Od { get; set; }
    public DateTime Do { get; set; }

    /// <summary>
    /// Koliko sati pripreme stoji izmedju dva najma. Nije dio blokova - blok pokazuje
    /// stvarni termin - nego podatak kojim interfejs moze nacrtati razmak i objasniti
    /// zasto termin odmah uz tudji najam nije slobodan.
    /// </summary>
    public double BufferSati { get; set; }

    public List<KalendarVoziloDto> Vozila { get; set; } = new();
}

public class KalendarVoziloDto
{
    public int VoziloId { get; set; }
    public string Vozilo { get; set; } = null!;
    public string RegistarskaOznaka { get; set; } = null!;
    public string TipVozila { get; set; } = null!;
    public string Poslovnica { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }

    public List<KalendarBlokDto> Blokovi { get; set; } = new();
}

/// <summary>Jedan zauzet raspon u redu jednog vozila.</summary>
public class KalendarBlokDto
{
    public VrstaBlokaKalendara Vrsta { get; set; }

    public DateTime Od { get; set; }
    public DateTime Do { get; set; }

    // --- kad je rijec o rezervaciji ---
    public int? RezervacijaId { get; set; }
    public string? Broj { get; set; }
    public string? Klijent { get; set; }
    public StatusRezervacije? Status { get; set; }

    /// <summary>Kod rezervacije koja ceka placanje: dokad termin ostaje rezervisan.</summary>
    public DateTime? DrziDo { get; set; }

    // --- kad je rijec o blokadi ---
    public int? BlokadaId { get; set; }
    public string? Razlog { get; set; }
}
