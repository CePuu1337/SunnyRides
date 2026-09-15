namespace SunnyRides.Model.DTOs;

/// <summary>
/// Odgovor na pitanje je li vozilo slobodno u zadatom terminu, sa razlogom kad nije.
///
/// Razlog se vraca zato sto klijentu "nije slobodno" ne govori nista - ne zna da li
/// da promijeni termin, vozilo ili oboje.
/// </summary>
public class DostupnostDto
{
    public int VoziloId { get; set; }
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }

    public bool Slobodno { get; set; }

    /// <summary>Prazno kad je vozilo slobodno.</summary>
    public string? Razlog { get; set; }

    public int BrojRezervacijaUTerminu { get; set; }
    public int BrojBlokadaUTerminu { get; set; }

    /// <summary>Razmak koji se dodaje sa obje strane termina, radi pripreme vozila. U satima.</summary>
    public double BufferSati { get; set; }
}
