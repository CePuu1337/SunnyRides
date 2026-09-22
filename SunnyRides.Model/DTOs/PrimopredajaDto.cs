using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Izdavanje ili povrat vozila. Fotografije se ne salju kao putanje, nego samo kao
/// identifikatori - sadrzaj se preuzima kroz endpoint koji provjerava vlasnistvo.
/// </summary>
public class PrimopredajaDto
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public string? RezervacijaBroj { get; set; }

    public TipPrimopredaje Tip { get; set; }
    /// <summary>Kad se primopredaja desila.</summary>
    public DateTime DatumVrijeme { get; set; }

    /// <summary>Kad je zapis unesen.</summary>
    public DateTime DatumUnosa { get; set; }

    /// <summary>Je li zapis unesen naknadno, a ne u trenutku primopredaje.</summary>
    public bool UnesenoNaknadno => DatumUnosa - DatumVrijeme > TimeSpan.FromMinutes(5);
    public int Kilometraza { get; set; }

    /// <summary>Procenat punog rezervoara, od 0 do 100.</summary>
    public int NivoGoriva { get; set; }

    public bool KontrolnaListaProdjena { get; set; }
    public string? Napomena { get; set; }
    public string? IzvrsioKorisnikIme { get; set; }

    public List<int> FotografijaIds { get; set; } = new();

    public bool ImaOstecenje { get; set; }
    public string? OpisStete { get; set; }
    public decimal? IznosStete { get; set; }
}
