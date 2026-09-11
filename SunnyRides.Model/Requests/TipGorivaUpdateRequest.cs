using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class TipGorivaUpdateRequest
{
    [Required(ErrorMessage = "Naziv tipa goriva je obavezan.")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Naziv tipa goriva mora imati izmedju 2 i 50 znakova.")]
    public string Naziv { get; set; } = null!;
}
