namespace SunnyRides.Model.DTOs;

/// <summary>
/// Iskoristenost flote kroz period, po vozilu.
///
/// Isti podatak sluzi za dvije stvari: prikaz prije generisanja, da korisnik provjeri
/// parametre, i sadrzaj PDF-a. Zato je ovo obican DTO, a PDF se gradi iz njega - da se
/// ne moze desiti da pregled pokaze jedno a dokument drugo.
/// </summary>
public class IskoristenostFloteDto
{
    public DateTime Od { get; set; }
    public DateTime Do { get; set; }

    /// <summary>Naziv poslovnice na koju je izvjestaj sveden, ili prazno za cijelu agenciju.</summary>
    public string? Poslovnica { get; set; }

    public DateTime GenerisanoUtc { get; set; }

    public List<StavkaIskoristenostiDto> Stavke { get; set; } = new();

    public ZbirIskoristenostiDto Zbir { get; set; } = new();

    public List<IskoristenostPoTipuDto> PoTipuVozila { get; set; } = new();
}

public class StavkaIskoristenostiDto
{
    public int VoziloId { get; set; }
    public string Vozilo { get; set; } = null!;
    public string RegistarskaOznaka { get; set; } = null!;
    public string TipVozila { get; set; } = null!;
    public string Poslovnica { get; set; } = null!;

    public int BrojNajmova { get; set; }

    /// <summary>Dani izdato u periodu. Racuna se iz stvarnih sati, pa zna imati decimalu.</summary>
    public double DanaIzdato { get; set; }

    /// <summary>Udio perioda u kojem je vozilo bilo kod klijenta, u procentima.</summary>
    public double Iskoristenost { get; set; }

    /// <summary>Stvarno naplaceno za najmove tog vozila u periodu, ne ugovorena vrijednost.</summary>
    public decimal Prihod { get; set; }

    public double? ProsjecnaOcjena { get; set; }
    public int BrojOcjena { get; set; }
}

public class ZbirIskoristenostiDto
{
    public int BrojVozila { get; set; }
    public int BrojNajmova { get; set; }
    public double DanaIzdato { get; set; }
    public double Iskoristenost { get; set; }
    public decimal Prihod { get; set; }
    public double? ProsjecnaOcjena { get; set; }
}

public class IskoristenostPoTipuDto
{
    public string TipVozila { get; set; } = null!;
    public int BrojVozila { get; set; }
    public int BrojNajmova { get; set; }
    public double DanaIzdato { get; set; }
    public double Iskoristenost { get; set; }
    public decimal Prihod { get; set; }
}
