using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Izmjena vlastitih podataka.
///
/// Nema polja za lozinku, ulogu ni status naloga. Lozinka ide kroz zasebnu radnju sa
/// starom lozinkom, a ulogu i status mijenja iskljucivo administrator - inace bi svako
/// mogao sam sebi ukloniti blokadu.
/// </summary>
public class ProfilUpdateRequest
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati izmedju 2 i 50 znakova.")]
    public string Ime { get; set; } = null!;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati izmedju 2 i 50 znakova.")]
    public string Prezime { get; set; } = null!;

    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [RegularExpression(@"^\+387 6\d{1} \d{3} \d{3}$",
        ErrorMessage = "Unesite broj telefona u formatu +387 6X XXX XXX.")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
    public DateTime DatumRodjenja { get; set; }
}
