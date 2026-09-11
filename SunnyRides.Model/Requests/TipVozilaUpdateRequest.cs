using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class TipVozilaUpdateRequest
{
    [Required(ErrorMessage = "Naziv tipa vozila je obavezan.")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Naziv tipa vozila mora imati izmedju 2 i 50 znakova.")]
    public string Naziv { get; set; } = null!;
}
