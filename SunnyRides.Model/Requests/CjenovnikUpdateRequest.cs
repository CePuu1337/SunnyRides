using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>Izmjena sezonske tarife.</summary>
public class CjenovnikUpdateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite model vozila.")]
    public int ModelVozilaId { get; set; }

    [Required(ErrorMessage = "Naziv tarife je obavezan.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Naziv tarife mora imati izmedju 2 i 100 znakova.")]
    public string Naziv { get; set; } = null!;

    [Required(ErrorMessage = "Pocetak perioda je obavezan.")]
    public DateTime DatumOd { get; set; }

    [Required(ErrorMessage = "Kraj perioda je obavezan.")]
    public DateTime DatumDo { get; set; }

    [Range(0.1, 10, ErrorMessage = "Mnozilac mora biti izmedju 0,1 i 10.")]
    public decimal Mnozilac { get; set; } = 1m;

    /// <summary>Prazno znaci da se koristi tarifa upisana na samom vozilu.</summary>
    [Range(0.01, 10_000, ErrorMessage = "Satna tarifa mora biti veca od nule.")]
    public decimal? SatnaTarifa { get; set; }

    [Range(0.01, 10_000, ErrorMessage = "Dnevna tarifa mora biti veca od nule.")]
    public decimal? DnevnaTarifa { get; set; }

    [Range(1, 365, ErrorMessage = "Prvi prag popusta mora biti izmedju 1 i 365 dana.")]
    public int PopustPrag1 { get; set; }

    [Range(0, 100, ErrorMessage = "Procenat popusta mora biti izmedju 0 i 100.")]
    public decimal PopustProcenat1 { get; set; }

    [Range(1, 365, ErrorMessage = "Drugi prag popusta mora biti izmedju 1 i 365 dana.")]
    public int PopustPrag2 { get; set; }

    [Range(0, 100, ErrorMessage = "Procenat popusta mora biti izmedju 0 i 100.")]
    public decimal PopustProcenat2 { get; set; }
}
