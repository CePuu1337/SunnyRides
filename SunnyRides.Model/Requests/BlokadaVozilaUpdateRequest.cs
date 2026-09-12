using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Izmjena blokade. Vozilo se ne mijenja - blokada na drugom vozilu je druga
/// blokada, pa se stara brise a nova unosi.
/// </summary>
public class BlokadaVozilaUpdateRequest
{
    [Required(ErrorMessage = "Datum pocetka blokade je obavezan.")]
    public DateTime DatumOd { get; set; }

    [Required(ErrorMessage = "Datum kraja blokade je obavezan.")]
    public DateTime DatumDo { get; set; }

    [Required(ErrorMessage = "Razlog blokade je obavezan.")]
    [StringLength(500, MinimumLength = 3,
        ErrorMessage = "Razlog mora imati izmedju 3 i 500 znakova.")]
    public string Razlog { get; set; } = null!;
}
