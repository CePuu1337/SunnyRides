using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Zahtjev za razradu cijene, prije nego rezervacija uopste postoji.
///
/// Namjerno nema nijedno polje sa iznosom. Klijent kaze sta hoce i za kada, a sve
/// sto ima cijenu server cita iz baze. Da iznos stize izvana, klijent bi mogao
/// rezervisati po cijeni koju sam odredi.
/// </summary>
public class ObracunCijeneRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite vozilo.")]
    public int VoziloId { get; set; }

    [Required(ErrorMessage = "Datum preuzimanja je obavezan.")]
    public DateTime DatumOd { get; set; }

    [Required(ErrorMessage = "Datum vracanja je obavezan.")]
    public DateTime DatumDo { get; set; }

    public List<StavkaOpremeRequest> Oprema { get; set; } = new();

    public int? PaketOsiguranjaId { get; set; }
}
