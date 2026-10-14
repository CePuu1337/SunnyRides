using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class GradInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite drzavu.")]
    public int DrzavaId { get; set; }

    [Required(ErrorMessage = "Naziv grada je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv grada mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [StringLength(20, ErrorMessage = "Postanski broj smije imati najvise 20 znakova.")]
    public string? PostanskiBroj { get; set; }

    /// <summary>
    /// Sredina grada, birana na karti. Opciona je, ali bez nje se klijentu iz ovog
    /// grada ne moze izracunati udaljenost do poslovnica.
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latituda mora biti izmedju -90 i 90.")]
    public double? Latituda { get; set; }

    [Range(-180, 180, ErrorMessage = "Longituda mora biti izmedju -180 i 180.")]
    public double? Longituda { get; set; }
}
