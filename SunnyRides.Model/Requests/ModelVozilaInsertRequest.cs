using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class ModelVozilaInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite marku.")]
    public int MarkaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite tip vozila.")]
    public int TipVozilaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite tip goriva.")]
    public int TipGorivaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite kategoriju vozacke dozvole.")]
    public int KategorijaDozvoleId { get; set; }

    [Required(ErrorMessage = "Naziv modela je obavezan.")]
    [StringLength(100, MinimumLength = 1,
        ErrorMessage = "Naziv modela mora imati izmedju 1 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [Range(0, 3000, ErrorMessage = "Kubikaza mora biti izmedju 0 i 3000 cm3.")]
    public int Kubikaza { get; set; }

    [Range(0.1, 300, ErrorMessage = "Snaga mora biti izmedju 0,1 i 300 kW.")]
    public decimal SnagaKw { get; set; }
}
