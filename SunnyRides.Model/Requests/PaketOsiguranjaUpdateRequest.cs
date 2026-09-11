using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class PaketOsiguranjaUpdateRequest
{
    [Required(ErrorMessage = "Naziv paketa je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv paketa mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [Range(0, 10000, ErrorMessage = "Cijena po danu ne smije biti negativna.")]
    public decimal CijenaPoDanu { get; set; }

    /// <summary>Iznos koji klijent snosi sam u slucaju stete. Nula znaci puno pokrice.</summary>
    [Range(0, 100000, ErrorMessage = "Iznos ucesca ne smije biti negativan.")]
    public decimal IznosUcesca { get; set; }
}
