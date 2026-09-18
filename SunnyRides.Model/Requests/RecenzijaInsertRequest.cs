using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Nova recenzija.
///
/// Zahtjev nosi samo rezervaciju, ocjenu i komentar. Vozilo i autor se izvode iz te
/// rezervacije - da ih klijent salje, mogao bi ocijeniti vozilo koje nije vozio ili
/// recenziju potpisati tudjim imenom.
/// </summary>
public class RecenzijaInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite rezervaciju na koju se recenzija odnosi.")]
    public int RezervacijaId { get; set; }

    [Range(1, 5, ErrorMessage = "Ocjena mora biti izmedju 1 i 5.")]
    public int Ocjena { get; set; }

    [MaxLength(1000, ErrorMessage = "Komentar moze imati najvise 1000 znakova.")]
    public string? Komentar { get; set; }
}
