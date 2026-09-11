using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class MarkaInsertRequest
{
    [Required(ErrorMessage = "Naziv marke je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv marke mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;
}
