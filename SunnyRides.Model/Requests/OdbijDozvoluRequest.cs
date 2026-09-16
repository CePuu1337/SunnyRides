using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Odbijanje dozvole. Razlog je obavezan - klijent mora znati sta da ispravi, a
/// uputstvo trazi da odbijanje od strane agencije uvijek nosi obrazlozenje.
/// Odobrenje nema svoj zahtjev jer razlog tamo nije potreban.
/// </summary>
public class OdbijDozvoluRequest
{
    [Required(ErrorMessage = "Razlog odbijanja je obavezan.")]
    [StringLength(500, MinimumLength = 5,
        ErrorMessage = "Razlog mora imati izmedju 5 i 500 znakova.")]
    public string Razlog { get; set; } = null!;
}
