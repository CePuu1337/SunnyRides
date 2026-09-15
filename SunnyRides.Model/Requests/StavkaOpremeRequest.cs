using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Jedna stavka opreme u zahtjevu. Klijent salje sta hoce i koliko, ali ne i po
/// kojoj cijeni - cijenu server cita iz sifrarnika.
/// </summary>
public class StavkaOpremeRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite vrstu opreme.")]
    public int VrstaOpremeId { get; set; }

    [Range(1, 10, ErrorMessage = "Kolicina mora biti izmedju 1 i 10.")]
    public int Kolicina { get; set; }
}
