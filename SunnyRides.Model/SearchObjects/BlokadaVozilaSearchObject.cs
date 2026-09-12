namespace SunnyRides.Model.SearchObjects;

public class BlokadaVozilaSearchObject : BaseSearchObject
{
    public int? VoziloId { get; set; }

    public string? RegistarskaOznaka { get; set; }

    /// <summary>Filtriranje po poslovnici ide kroz vozilo.</summary>
    public int? PoslovnicaId { get; set; }

    public string? Razlog { get; set; }

    /// <summary>Blokade koje se preklapaju sa zadatim periodom.</summary>
    public DateTime? PeriodOd { get; set; }
    public DateTime? PeriodDo { get; set; }

    /// <summary>True vraca samo blokade koje jos traju ili tek dolaze.</summary>
    public bool? SamoAktivne { get; set; }
}
