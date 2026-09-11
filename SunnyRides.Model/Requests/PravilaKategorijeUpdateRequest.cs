using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class PravilaKategorijeUpdateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite kategoriju vozacke dozvole.")]
    public int KategorijaDozvoleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite tip vozila.")]
    public int TipVozilaId { get; set; }

    /// <summary>Prazno znaci bez ogranicenja - kategorija A pokriva sve kubikaze.</summary>
    [Range(1, 3000, ErrorMessage = "Maksimalna kubikaza mora biti izmedju 1 i 3000 cm3.")]
    public int? MaxKubikaza { get; set; }

    /// <summary>Prazno znaci bez ogranicenja.</summary>
    [Range(0.1, 300, ErrorMessage = "Maksimalna snaga mora biti izmedju 0,1 i 300 kW.")]
    public decimal? MaxSnagaKw { get; set; }

    [Range(14, 99, ErrorMessage = "Minimalna dob mora biti izmedju 14 i 99 godina.")]
    public int MinGodine { get; set; }
}
