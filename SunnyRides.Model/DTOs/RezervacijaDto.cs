using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

public class RezervacijaDto
{
    public int Id { get; set; }
    public string Broj { get; set; } = null!;

    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public StatusRezervacije Status { get; set; }

    public decimal UkupanIznos { get; set; }
    public decimal IznosDepozita { get; set; }
    public decimal IznosPopusta { get; set; }

    /// <summary>Klijentska aplikacija po ovome odlucuje hoce li uopste prikazati dugme za placanje.</summary>
    public bool IsPaid { get; set; }

    /// <summary>Do kada vozilo ostaje rezervisano bez placanja. Prazno kad drzanje vise nije bitno.</summary>
    public DateTime? DrziDo { get; set; }

    /// <summary>
    /// Koliko sekundi jos traje drzanje. Racuna ga server, da odbrojavanje na ekranu
    /// ne zavisi od toga koliko je sat na uredjaju tacan.
    /// </summary>
    public int? PreostaloSekundiDrzanja { get; set; }

    public DateTime DatumKreiranja { get; set; }
    public int? RazlogOtkazivanjaId { get; set; }

    /// <summary>Prazno kad je rezervaciju otkazao sistem - tada je objasnjenje u napomeni.</summary>
    public string? RazlogOtkazivanjaNaziv { get; set; }

    public string? NapomenaOtkazivanja { get; set; }
    public DateTime? DatumOtkazivanja { get; set; }
    public string? OtkazaoKorisnikIme { get; set; }

    public int KorisnikId { get; set; }
    public string? KlijentImePrezime { get; set; }
    public string? KlijentEmail { get; set; }

    public int VoziloId { get; set; }
    public string? RegistarskaOznaka { get; set; }
    public string? ModelNaziv { get; set; }
    public string? MarkaNaziv { get; set; }
    public string? ThumbnailUrl { get; set; }

    public int PoslovnicaId { get; set; }
    public string? PoslovnicaNaziv { get; set; }

    public int? PaketOsiguranjaId { get; set; }
    public string? PaketOsiguranjaNaziv { get; set; }

    public List<StavkaOpremeDto> StavkeOpreme { get; set; } = new();

    /// <summary>Puni se samo na detaljnom dohvatu - lista rezervacija ne vuce historiju.</summary>
    public List<HistorijaStatusaDto> HistorijaStatusa { get; set; } = new();
}
