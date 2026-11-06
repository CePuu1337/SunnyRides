using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>Zalihe jedne vrste opreme u jednoj poslovnici.</summary>
public class StanjeOpremeInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite vrstu opreme.")]
    public int VrstaOpremeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite poslovnicu.")]
    public int PoslovnicaId { get; set; }

    [Range(0, 1000, ErrorMessage = "Kolicina mora biti izmedju 0 i 1000.")]
    public int Kolicina { get; set; }
}
