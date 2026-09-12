using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

public class VoziloUpdateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite model vozila.")]
    public int ModelVozilaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite poslovnicu.")]
    public int PoslovnicaId { get; set; }

    [Required(ErrorMessage = "Registarska oznaka je obavezna.")]
    [StringLength(20, MinimumLength = 5,
        ErrorMessage = "Registarska oznaka mora imati izmedju 5 i 20 znakova.")]
    public string RegistarskaOznaka { get; set; } = null!;

    [Range(1990, 2100, ErrorMessage = "Godina proizvodnje mora biti izmedju 1990. i tekuce godine.")]
    public int GodinaProizvodnje { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Kilometraza mora biti izmedju 0 i 1000000 km.")]
    public int Kilometraza { get; set; }

    [Range(0.01, 10_000, ErrorMessage = "Satna tarifa mora biti veca od nule.")]
    public decimal SatnaTarifa { get; set; }

    [Range(0.01, 10_000, ErrorMessage = "Dnevna tarifa mora biti veca od nule.")]
    public decimal DnevnaTarifa { get; set; }

    [Range(0, 100_000, ErrorMessage = "Iznos depozita ne smije biti negativan.")]
    public decimal IznosDepozita { get; set; }

    /// <summary>
    /// Vozilo sa rezervacijama se ne brise nego deaktivira. Deaktivirano vozilo
    /// nestaje iz pretrage i kalendara flote, a historija najmova ostaje netaknuta.
    /// </summary>
    public bool Aktivno { get; set; } = true;
}
