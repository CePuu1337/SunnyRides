using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Izmjena zaliha. Mijenja se samo kolicina: oprema i poslovnica odredjuju o kojem
/// se zapisu radi, pa bi njihova izmjena bila zapravo novi zapis.
/// </summary>
public class StanjeOpremeUpdateRequest
{
    [Range(0, 1000, ErrorMessage = "Kolicina mora biti izmedju 0 i 1000.")]
    public int Kolicina { get; set; }
}
