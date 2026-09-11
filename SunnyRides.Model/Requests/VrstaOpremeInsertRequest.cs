using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class VrstaOpremeInsertRequest
{
    [Required(ErrorMessage = "Naziv opreme je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv opreme mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    /// <summary>Popunjava se kad se oprema naplacuje po danu najma.</summary>
    [Range(0.01, 10000, ErrorMessage = "Cijena po danu mora biti veca od nule.")]
    public decimal? CijenaPoDanu { get; set; }

    /// <summary>Popunjava se kad se oprema naplacuje jednom, bez obzira na trajanje najma.</summary>
    [Range(0.01, 10000, ErrorMessage = "Fiksna cijena mora biti veca od nule.")]
    public decimal? FiksnaCijena { get; set; }
}
