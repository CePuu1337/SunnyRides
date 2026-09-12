using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Nova blokada vozila.
///
/// Namjerno nema polje <c>KreiraoKorisnikId</c>. Ko je blokadu evidentirao je
/// podatak o tome ko poziva operaciju, a to se uvijek cita iz tokena. Da stize iz
/// zahtjeva, svaki uposlenik mogao bi blokadu pripisati kolegi.
/// </summary>
public class BlokadaVozilaInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite vozilo.")]
    public int VoziloId { get; set; }

    [Required(ErrorMessage = "Datum pocetka blokade je obavezan.")]
    public DateTime DatumOd { get; set; }

    [Required(ErrorMessage = "Datum kraja blokade je obavezan.")]
    public DateTime DatumDo { get; set; }

    [Required(ErrorMessage = "Razlog blokade je obavezan.")]
    [StringLength(500, MinimumLength = 3,
        ErrorMessage = "Razlog mora imati izmedju 3 i 500 znakova.")]
    public string Razlog { get; set; } = null!;
}
