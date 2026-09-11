using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Korisnik mijenja vlastitu lozinku, pa se trazi i stara. Administratorski reset
/// lozinke je zasebna operacija koja staru lozinku ne trazi.
/// </summary>
public class PromjenaLozinkeRequest
{
    [Required(ErrorMessage = "Stara lozinka je obavezna.")]
    public string StaraLozinka { get; set; } = null!;

    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Nova lozinka mora imati najmanje 6 znakova.")]
    public string NovaLozinka { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda nove lozinke je obavezna.")]
    [Compare(nameof(NovaLozinka), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string PotvrdaNoveLozinke { get; set; } = null!;
}
