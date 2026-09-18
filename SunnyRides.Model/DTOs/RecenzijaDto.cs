namespace SunnyRides.Model.DTOs;

/// <summary>
/// Ocjena i komentar nakon zavrsenog najma.
///
/// Klijent vidi svoje i tudje neskrivene recenzije uz vozilo; osoblje vidi sve, jer
/// moderira. Zato je autor ovdje imenom i prezimenom, a ne samo identifikatorom.
/// </summary>
public class RecenzijaDto
{
    public int Id { get; set; }

    public int KorisnikId { get; set; }
    public string? KorisnikImePrezime { get; set; }

    public int VoziloId { get; set; }
    public string? VoziloOpis { get; set; }
    public string? RegistarskaOznaka { get; set; }

    public int RezervacijaId { get; set; }
    public string? RezervacijaBroj { get; set; }

    public int Ocjena { get; set; }
    public string? Komentar { get; set; }
    public DateTime DatumKreiranja { get; set; }

    /// <summary>
    /// Skrivena recenzija ne ulazi u prosjecnu ocjenu ni u sistem preporuke. Klijentu
    /// se prikazuje samo njegova vlastita skrivena recenzija, da zna sta se desilo.
    /// </summary>
    public bool Skrivena { get; set; }
}
