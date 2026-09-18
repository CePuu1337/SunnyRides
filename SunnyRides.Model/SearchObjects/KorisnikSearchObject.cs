namespace SunnyRides.Model.SearchObjects;

public class KorisnikSearchObject : BaseSearchObject
{
    /// <summary>Trazi se po imenu, prezimenu, korisnickom imenu i email adresi odjednom.</summary>
    public string? Tekst { get; set; }

    /// <summary>Naziv uloge, ne identifikator - isti oblik koji stoji i u odgovoru.</summary>
    public string? Uloga { get; set; }

    public bool? Aktivan { get; set; }
    public bool? Blokiran { get; set; }
}
