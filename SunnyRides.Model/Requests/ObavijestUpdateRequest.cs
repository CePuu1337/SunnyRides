using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class ObavijestUpdateRequest
{
    [Required(ErrorMessage = "Unesite naslov obavijesti.")]
    [MaxLength(200, ErrorMessage = "Naslov moze imati najvise 200 znakova.")]
    public string Naslov { get; set; } = null!;

    [Required(ErrorMessage = "Unesite tekst obavijesti.")]
    [MaxLength(2000, ErrorMessage = "Tekst moze imati najvise 2000 znakova.")]
    public string Tekst { get; set; } = null!;

    public DateTime? DatumObjave { get; set; }

    public bool Aktivna { get; set; }
}
