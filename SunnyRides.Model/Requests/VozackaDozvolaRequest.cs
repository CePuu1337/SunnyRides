using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Prijava vozacke dozvole. Klijent je salje za sebe - zahtjev nema polje
/// <c>KorisnikId</c>, jer se vlasnik cita iz tokena.
///
/// Nema ni polje <c>Status</c>: nova ili izmijenjena dozvola uvijek ide na cekanje,
/// a odobrava je uposlenik. Da status stize izvana, klijent bi sam sebi odobrio
/// dozvolu i preskocio verifikaciju.
/// </summary>
public class VozackaDozvolaRequest
{
    [Required(ErrorMessage = "Broj dozvole je obavezan.")]
    [StringLength(50, MinimumLength = 4,
        ErrorMessage = "Broj dozvole mora imati izmedju 4 i 50 znakova.")]
    public string BrojDozvole { get; set; } = null!;

    [Required(ErrorMessage = "Datum izdavanja je obavezan.")]
    public DateTime DatumIzdavanja { get; set; }

    [Required(ErrorMessage = "Datum isteka je obavezan.")]
    public DateTime DatumIsteka { get; set; }

    [MinLength(1, ErrorMessage = "Odaberite najmanje jednu kategoriju.")]
    public List<int> KategorijaIds { get; set; } = new();
}
