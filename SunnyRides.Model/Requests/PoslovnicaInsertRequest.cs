using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class PoslovnicaInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite grad.")]
    public int GradId { get; set; }

    [Required(ErrorMessage = "Naziv poslovnice je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv poslovnice mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [Required(ErrorMessage = "Adresa je obavezna.")]
    [StringLength(200, MinimumLength = 3,
        ErrorMessage = "Adresa mora imati izmedju 3 i 200 znakova.")]
    public string Adresa { get; set; } = null!;

    [Range(-90, 90, ErrorMessage = "Latituda mora biti izmedju -90 i 90.")]
    public double? Latituda { get; set; }

    [Range(-180, 180, ErrorMessage = "Longituda mora biti izmedju -180 i 180.")]
    public double? Longituda { get; set; }

    [StringLength(100, ErrorMessage = "Radno vrijeme smije imati najvise 100 znakova.")]
    public string? RadnoVrijeme { get; set; }
}
