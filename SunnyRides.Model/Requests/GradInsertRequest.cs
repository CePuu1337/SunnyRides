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
}
