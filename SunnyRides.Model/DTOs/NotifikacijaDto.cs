using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Jedno obavjestenje u listi korisnika.
///
/// KorisnikId namjerno nije u odgovoru: lista i detalj vracaju iskljucivo zapise
/// prijavljenog korisnika, pa bi to polje bilo isto u svakom redu. Kad ga nema, nema
/// ni nacina da klijentska aplikacija dodje na ideju da ga negdje posalje nazad.
/// </summary>
public class NotifikacijaDto
{
    public int Id { get; set; }

    public string Naslov { get; set; } = null!;

    public string Tekst { get; set; } = null!;

    public TipNotifikacije Tip { get; set; }

    public bool Procitana { get; set; }

    public DateTime DatumKreiranja { get; set; }

    /// <summary>
    /// Rezervacija na koju se obavjestenje odnosi, ako je ima. Aplikacija po ovome
    /// otvara detalje rezervacije kad korisnik dodirne obavjestenje.
    /// </summary>
    public int? RezervacijaId { get; set; }

    /// <summary>Broj rezervacije, da lista ne mora prikazivati identifikator.</summary>
    public string? RezervacijaBroj { get; set; }
}
