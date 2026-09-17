using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Povrat vozila. Iznos povrata depozita se ne salje - racuna ga server iz uplate,
/// stete i eventualnog kasnjenja.
/// </summary>
public class PovratVozilaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite rezervaciju.")]
    public int RezervacijaId { get; set; }

    [Range(0, 2_000_000, ErrorMessage = "Kilometraza mora biti izmedju 0 i 2.000.000 km.")]
    public int Kilometraza { get; set; }

    [Range(0, 100, ErrorMessage = "Nivo goriva unosi se u procentima, od 0 do 100.")]
    public int NivoGoriva { get; set; }

    public bool ImaOstecenje { get; set; }

    /// <summary>Obavezan kad je oznaceno ostecenje.</summary>
    [MaxLength(1000, ErrorMessage = "Opis stete moze imati najvise 1000 znakova.")]
    public string? OpisStete { get; set; }

    /// <summary>Obavezan kad je oznaceno ostecenje. Umanjuje povrat depozita.</summary>
    [Range(0, 100_000, ErrorMessage = "Iznos stete mora biti izmedju 0 i 100.000 EUR.")]
    public decimal? IznosStete { get; set; }

    [MaxLength(1000, ErrorMessage = "Napomena moze imati najvise 1000 znakova.")]
    public string? Napomena { get; set; }

    /// <summary>
    /// Kad je vozilo za popravku, uposlenik ga ovdje odmah blokira do navedenog
    /// datuma, pa ga pretraga vise ne nudi.
    /// </summary>
    public DateTime? BlokirajVoziloDo { get; set; }
}
