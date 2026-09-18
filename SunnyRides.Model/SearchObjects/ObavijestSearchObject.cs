namespace SunnyRides.Model.SearchObjects;

public class ObavijestSearchObject : BaseSearchObject
{
    /// <summary>Trazi se i po naslovu i po tekstu.</summary>
    public string? Tekst { get; set; }

    /// <summary>
    /// Filter za administratora. Klijentu ne znaci nista - njemu se neaktivne i
    /// zakazane obavijesti ne prikazuju bez obzira sta posalje.
    /// </summary>
    public bool? Aktivna { get; set; }

    public DateTime? OdDatuma { get; set; }
    public DateTime? DoDatuma { get; set; }
}
