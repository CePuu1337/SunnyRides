using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class ObavijestInsertRequest
{
    [Required(ErrorMessage = "Unesite naslov obavijesti.")]
    [MaxLength(200, ErrorMessage = "Naslov moze imati najvise 200 znakova.")]
    public string Naslov { get; set; } = null!;

    [Required(ErrorMessage = "Unesite tekst obavijesti.")]
    [MaxLength(2000, ErrorMessage = "Tekst moze imati najvise 2000 znakova.")]
    public string Tekst { get; set; } = null!;

    /// <summary>
    /// Kad obavijest postaje vidljiva. Prazno znaci odmah. Datum u buducnosti je
    /// zakazana objava - klijenti je do tada ne vide.
    /// </summary>
    public DateTime? DatumObjave { get; set; }

    public bool Aktivna { get; set; } = true;
}
