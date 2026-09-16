using SunnyRides.Model.Enums;

namespace SunnyRides.Model.SearchObjects;

public class RezervacijaSearchObject : BaseSearchObject
{
    public string? Broj { get; set; }

    public StatusRezervacije? Status { get; set; }

    /// <summary>Dio imena, prezimena ili emaila klijenta. Klijentu se ignorise - on ionako vidi samo svoje.</summary>
    public string? Klijent { get; set; }

    public string? RegistarskaOznaka { get; set; }

    public int? VoziloId { get; set; }
    public int? PoslovnicaId { get; set; }

    /// <summary>
    /// Rezervacije koje se preklapaju sa zadatim periodom. Kalendar flote ovim
    /// dohvata sedmicu koju prikazuje.
    /// </summary>
    public DateTime? PeriodOd { get; set; }
    public DateTime? PeriodDo { get; set; }

    public bool? IsPaid { get; set; }

    /// <summary>
    /// True vraca samo rezervacije koje jos traju ili tek dolaze - tab "Aktivne" u
    /// mobilnoj aplikaciji. False vraca historiju.
    /// </summary>
    public bool? SamoAktivne { get; set; }
}
