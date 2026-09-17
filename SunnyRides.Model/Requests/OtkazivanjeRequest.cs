using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Otkazivanje rezervacije.
///
/// Zahtjev nosi samo odabrani razlog i eventualnu napomenu. Iznos povrata, status i
/// to ko otkazuje ne dolaze od klijenta: iznos racuna server iz stvarno naplacenog,
/// status mijenja state machine, a korisnik se cita iz tokena. Da ovdje postoji polje
/// za iznos povrata, klijent bi dan prije termina mogao sam upisati puni povrat.
/// </summary>
public class OtkazivanjeRequest
{
    /// <summary>Razlog iz padajuce liste, i za klijenta i za agenciju.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Odaberite razlog otkazivanja.")]
    public int RazlogOtkazivanjaId { get; set; }

    /// <summary>Dodatno objasnjenje. Obavezno samo uz razloge koji ga traze, npr. "Ostalo".</summary>
    [MaxLength(500, ErrorMessage = "Napomena moze imati najvise 500 znakova.")]
    public string? Napomena { get; set; }
}
