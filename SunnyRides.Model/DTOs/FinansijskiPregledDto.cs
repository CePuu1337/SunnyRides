namespace SunnyRides.Model.DTOs;

/// <summary>
/// Finansijski pregled po mjesecima i poslovnicama.
///
/// Sve brojke dolaze iz **placanja**, ne iz iznosa rezervacija. Rezervacija nosi koliko
/// je trebalo naplatiti, a placanje koliko jeste - a izvjestaj o prihodu mora govoriti o
/// novcu koji je stvarno presao.
/// </summary>
public class FinansijskiPregledDto
{
    public DateTime Od { get; set; }
    public DateTime Do { get; set; }

    public string? Poslovnica { get; set; }

    public DateTime GenerisanoUtc { get; set; }

    public List<StavkaFinansijskogDto> Stavke { get; set; } = new();

    public ZbirFinansijskogDto Zbir { get; set; } = new();
}

public class StavkaFinansijskogDto
{
    public int Godina { get; set; }
    public int Mjesec { get; set; }

    /// <summary>Naziv mjeseca i godina, spremno za ispis.</summary>
    public string Period { get; set; } = null!;

    public int PoslovnicaId { get; set; }
    public string Poslovnica { get; set; } = null!;

    /// <summary>Broj rezervacija koje su u tom mjesecu bile placene.</summary>
    public int BrojRezervacija { get; set; }

    public decimal Naplaceno { get; set; }
    public decimal Refundirano { get; set; }
    public decimal NetoPrihod { get; set; }

    /// <summary>Naplaceno podijeljeno brojem rezervacija.</summary>
    public decimal ProsjecnaVrijednostNajma { get; set; }
}

public class ZbirFinansijskogDto
{
    public int BrojRezervacija { get; set; }
    public decimal Naplaceno { get; set; }
    public decimal Refundirano { get; set; }
    public decimal NetoPrihod { get; set; }
    public decimal ProsjecnaVrijednostNajma { get; set; }
}
