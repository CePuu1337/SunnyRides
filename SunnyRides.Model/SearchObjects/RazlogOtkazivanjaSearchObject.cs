namespace SunnyRides.Model.SearchObjects;

public class RazlogOtkazivanjaSearchObject : BaseSearchObject
{
    public string? Naziv { get; set; }

    /// <summary>Mobilna aplikacija salje true i dobija samo razloge koje klijent smije izabrati.</summary>
    public bool? ZaKlijenta { get; set; }

    /// <summary>Desktop salje true kad agencija otkazuje.</summary>
    public bool? ZaAgenciju { get; set; }

    /// <summary>Padajuce liste salju true, a administracija sifrarnika gleda sve.</summary>
    public bool? Aktivan { get; set; }
}
