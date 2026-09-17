namespace SunnyRides.Model.DTOs;

/// <summary>
/// Obracun depozita pri povratu vozila. Uposlenik ga vidi uz formu prije nego
/// potvrdi povrat, a isti obracun se radi i pri samom upisu.
/// </summary>
public class ObracunPovrataDto
{
    public int RezervacijaId { get; set; }
    public string? Broj { get; set; }

    public DateTime UgovorenoVracanje { get; set; }
    public DateTime DatumPovrata { get; set; }

    /// <summary>Koliko je vozilo vraceno kasnije od ugovorenog. Nula kad je vraceno na vrijeme ili ranije.</summary>
    public int KasnjenjeMinuta { get; set; }

    /// <summary>True kad je kasnjenje unutar dozvoljenih 59 minuta, pa se ne naplacuje.</summary>
    public bool UnutarTolerancije { get; set; }

    public int DanaPrekoracenja { get; set; }
    public decimal DnevnaCijena { get; set; }
    public decimal Doplata { get; set; }

    public decimal IznosStete { get; set; }

    /// <summary>Dio uplate koji je depozit i jos nije vracen.</summary>
    public decimal UplaceniDepozit { get; set; }

    public decimal ZadrzanoOdDepozita { get; set; }
    public decimal PovratDepozita { get; set; }

    /// <summary>Dio stete i doplate koji depozit ne pokriva. Agencija ga naplacuje van ovog sistema.</summary>
    public decimal NepokrivenoDepozitom { get; set; }

    public string Obrazlozenje { get; set; } = null!;

    // Podaci sa izdavanja, za poredjenje na formi za povrat.
    public int? KilometrazaPriIzdavanju { get; set; }
    public int? NivoGorivaPriIzdavanju { get; set; }
    public DateTime? DatumIzdavanja { get; set; }
    public string? IzdaoKorisnikIme { get; set; }
    public int BrojFotografijaPriIzdavanju { get; set; }
}
