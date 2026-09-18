namespace SunnyRides.Model.SearchObjects;

public class RecenzijaSearchObject : BaseSearchObject
{
    public int? VoziloId { get; set; }
    public int? ModelVozilaId { get; set; }
    public int? RezervacijaId { get; set; }

    /// <summary>Pretraga po klijentu je za osoblje; klijentu je lista ionako suzena.</summary>
    public string? Klijent { get; set; }

    /// <summary>Filter za moderaciju. Klijent ovim ne moze doci do tudjih skrivenih recenzija.</summary>
    public bool? Skrivena { get; set; }

    public int? OcjenaOd { get; set; }
    public int? OcjenaDo { get; set; }

    /// <summary>Samo vlastite recenzije - za ekran "moje recenzije" u mobilnoj aplikaciji.</summary>
    public bool SamoMoje { get; set; }

    public string? Komentar { get; set; }
}
