using System.ComponentModel.DataAnnotations;

namespace SunnyRides.Model.Requests;

/// <summary>
/// Otkazivanje rezervacije.
///
/// Zahtjev nosi samo razlog. Ni iznos povrata, ni status, ni ko otkazuje ne dolaze
/// izvana: iznos racuna server iz stvarno naplacenog, status postavlja state machine,
/// a izvrsilac se cita iz tokena. Da polje "iznos povrata" postoji ovdje, klijent bi
/// mogao otkazati dan prije termina i sam upisati puni povrat.
/// </summary>
public class OtkazivanjeRequest
{
    /// <summary>
    /// Obavezan kad otkazuje osoblje - klijent ima pravo znati zasto mu je agencija
    /// otkazala najam. Klijent svoj razlog ne mora navesti.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Razlog moze imati najvise 500 znakova.")]
    public string? Razlog { get; set; }
}
