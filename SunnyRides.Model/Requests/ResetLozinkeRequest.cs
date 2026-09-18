using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Drugi korak reseta lozinke: kod iz emaila i nova lozinka. Stara lozinka se ne
/// trazi - ovaj put i postoji zato sto je korisnik ne zna.
/// </summary>
public class ResetLozinkeRequest
{
    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Email adresa nije u ispravnom obliku.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Kod iz emaila je obavezan.")]
    public string Kod { get; set; } = null!;

    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Nova lozinka mora imati najmanje 6 znakova.")]
    public string NovaLozinka { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda nove lozinke je obavezna.")]
    [Compare(nameof(NovaLozinka), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string PotvrdaNoveLozinke { get; set; } = null!;
}
