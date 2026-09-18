using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>Izmjena vlastite recenzije. Rezervacija i vozilo se ne mijenjaju.</summary>
public class RecenzijaUpdateRequest
{
    [Range(1, 5, ErrorMessage = "Ocjena mora biti izmedju 1 i 5.")]
    public int Ocjena { get; set; }

    [MaxLength(1000, ErrorMessage = "Komentar moze imati najvise 1000 znakova.")]
    public string? Komentar { get; set; }
}
