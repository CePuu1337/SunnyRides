using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Administratorski reset lozinke.
///
/// Namjerno **ne trazi staru lozinku** - administrator je ne zna i ne treba je znati.
/// Kad korisnik mijenja vlastitu lozinku, stara se trazi; to je druga radnja i drugi
/// zahtjev.
/// </summary>
public class AdminResetLozinkeRequest
{
    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati najmanje 6 znakova.")]
    public string NovaLozinka { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
    [Compare(nameof(NovaLozinka), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string PotvrdaNoveLozinke { get; set; } = null!;
}
