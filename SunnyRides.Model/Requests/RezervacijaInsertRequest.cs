using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Nova rezervacija.
///
/// Zahtjev nema nijedno polje koje nosi novac ni stanje: nema <c>UkupanIznos</c>,
/// <c>IznosDepozita</c>, <c>IznosPopusta</c>, <c>Status</c>, <c>IsPaid</c> ni
/// <c>DrziDo</c>. Sve to racuna i postavlja server.
///
/// Nema ni <c>KorisnikId</c> - klijent rezervise za sebe, a ko je to cita se iz tokena.
/// Nema ni <c>PoslovnicaId</c>, jer se preuzima tamo gdje vozilo jeste.
/// </summary>
public class RezervacijaInsertRequest
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
