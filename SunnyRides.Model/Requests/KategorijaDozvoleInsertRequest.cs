using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class KategorijaDozvoleInsertRequest
{
    [Required(ErrorMessage = "Oznaka kategorije je obavezna.")]
    [StringLength(10, MinimumLength = 1,
        ErrorMessage = "Oznaka kategorije mora imati izmedju 1 i 10 znakova.")]
    public string Oznaka { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Opis smije imati najvise 200 znakova.")]
    public string? Opis { get; set; }
}
