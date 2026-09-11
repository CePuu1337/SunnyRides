using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Kredencijali idu iskljucivo u tijelu POST zahtjeva, nikad kroz query string.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Korisnicko ime je obavezno.")]
    public string KorisnickoIme { get; set; } = null!;

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    public string Lozinka { get; set; } = null!;
}
