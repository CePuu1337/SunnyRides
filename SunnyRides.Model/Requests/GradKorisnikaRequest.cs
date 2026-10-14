using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Izbor vlastitog grada, bez ostalih podataka profila. Mobilna aplikacija ga salje
/// odmah poslije registracije, kad klijent jos nema grad.
/// </summary>
public class GradKorisnikaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite grad.")]
    public int GradId { get; set; }
}
