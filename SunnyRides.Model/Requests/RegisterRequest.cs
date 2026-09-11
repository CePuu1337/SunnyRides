using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Registracija kroz mobilnu aplikaciju. Novi nalog uvijek dobija iskljucivo ulogu
/// Klijent - zato ovaj zahtjev namjerno NEMA polja Role, RoleId ni IsAdmin. Da ih ima,
/// klijent bi sam sebi mogao dodijeliti administratorska prava.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Korisnicko ime je obavezno.")]
    [StringLength(50, MinimumLength = 3,
        ErrorMessage = "Korisnicko ime mora imati izmedju 3 i 50 znakova.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Korisnicko ime smije sadrzavati samo slova, brojeve, tacku, crticu i donju crtu.")]
    public string KorisnickoIme { get; set; } = null!;

    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati izmedju 2 i 50 znakova.")]
    public string Ime { get; set; } = null!;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati izmedju 2 i 50 znakova.")]
    public string Prezime { get; set; } = null!;

    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu, na primjer ime.prezime@gmail.com.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Broj telefona je obavezan.")]
    [RegularExpression(@"^\+387 6\d{1} \d{3} \d{3}$",
        ErrorMessage = "Unesite broj telefona u formatu +387 6X XXX XXX.")]
    public string Telefon { get; set; } = null!;

    [Required(ErrorMessage = "Datum rodjenja je obavezan.")]
    public DateTime DatumRodjenja { get; set; }

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Lozinka mora imati najmanje 6 znakova.")]
    public string Lozinka { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
    [Compare(nameof(Lozinka), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string PotvrdaLozinke { get; set; } = null!;
}
