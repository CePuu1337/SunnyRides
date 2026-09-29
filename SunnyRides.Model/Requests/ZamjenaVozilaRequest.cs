using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Zamjena vozila na rezervaciji, kad je dogovoreno vozilo u kvaru ili na servisu.
///
/// Nosi samo novo vozilo i napomenu. Cijena se ne salje i ne mijenja - klijent placa
/// ono sto je rezervisao, a da li je novo vozilo iste ili bolje klase provjerava server.
/// </summary>
public class ZamjenaVozilaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite zamjensko vozilo.")]
    public int NovoVoziloId { get; set; }

    /// <summary>Zasto se vozilo mijenja - ide u historiju rezervacije i klijentu u obavjestenje.</summary>
    [MaxLength(500, ErrorMessage = "Napomena moze imati najvise 500 znakova.")]
    public string? Napomena { get; set; }
}
