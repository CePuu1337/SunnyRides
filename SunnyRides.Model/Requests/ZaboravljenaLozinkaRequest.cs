using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Prvi korak reseta lozinke: klijent upisuje email, a na njega stize kod.
/// </summary>
public class ZaboravljenaLozinkaRequest
{
    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Email adresa nije u ispravnom obliku.")]
    public string Email { get; set; } = null!;
}
