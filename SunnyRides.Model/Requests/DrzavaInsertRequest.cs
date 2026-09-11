using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class DrzavaInsertRequest
{
    [Required(ErrorMessage = "Naziv drzave je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv drzave mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [Required(ErrorMessage = "Skracenica je obavezna.")]
    [StringLength(10, MinimumLength = 2,
        ErrorMessage = "Skracenica mora imati izmedju 2 i 10 znakova.")]
    public string Skracenica { get; set; } = null!;
}
