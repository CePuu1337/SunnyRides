using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class RazlogOtkazivanjaUpdateRequest
{
    [Required(ErrorMessage = "Naziv razloga je obavezan.")]
    [StringLength(100, MinimumLength = 3,
        ErrorMessage = "Naziv razloga mora imati izmedju 3 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    /// <summary>Razlog se nudi klijentu u mobilnoj aplikaciji.</summary>
    public bool ZaKlijenta { get; set; }

    /// <summary>Razlog se nudi osoblju kad agencija otkazuje rezervaciju.</summary>
    public bool ZaAgenciju { get; set; }

    public bool TraziNapomenu { get; set; }

    public bool Aktivan { get; set; } = true;
}
