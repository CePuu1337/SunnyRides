using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Izdavanje vozila klijentu. Vrijeme i uposlenika ne salje aplikacija - server ih
/// upisuje sam, iz sata na serveru i iz tokena.
/// </summary>
public class IzdavanjeVozilaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite rezervaciju.")]
    public int RezervacijaId { get; set; }

    [Range(0, 2_000_000, ErrorMessage = "Kilometraza mora biti izmedju 0 i 2.000.000 km.")]
    public int Kilometraza { get; set; }

    [Range(0, 100, ErrorMessage = "Nivo goriva unosi se u procentima, od 0 do 100.")]
    public int NivoGoriva { get; set; }

    /// <summary>Uposlenik potvrdjuje da je prosao kontrolnu listu stanja vozila.</summary>
    public bool KontrolnaListaProdjena { get; set; }

    [MaxLength(1000, ErrorMessage = "Napomena moze imati najvise 1000 znakova.")]
    public string? Napomena { get; set; }
}
