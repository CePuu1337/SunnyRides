using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Sta se desava ako se rezervacija sada otkaze - razrada, ne samo konacan iznos.
///
/// Klijentska aplikacija ovo trazi **prije** nego pokaze dugme za otkazivanje, pa
/// korisnik vidi koliko gubi prije nego potvrdi. Isti obracun se zatim ponovo radi
/// na serveru pri samom otkazivanju: ovaj poziv je prikaz, a ne obecanje. Kad bi se
/// vjerovalo ovom odgovoru, klijent bi ga mogao dobiti dok je do preuzimanja bilo
/// osam dana, a otkazati dan prije termina i traziti puni povrat.
/// </summary>
public class ObracunOtkazivanjaDto
{
    public int RezervacijaId { get; set; }
    public string? Broj { get; set; }
    public StatusRezervacije Status { get; set; }

    /// <summary>Termin preuzimanja - od njega se mjeri koliko je otkazivanje rano.</summary>
    public DateTime DatumOd { get; set; }

    /// <summary>
    /// False kad je rezervacija vec otkazana ili zavrsena, ili je vozilo vec izdato.
    /// Tada su svi iznosi nule, a razlog stoji u polju ispod.
    /// </summary>
    public bool MozeSeOtkazati { get; set; }

    public string? RazlogNemogucnosti { get; set; }

    /// <summary>Koliko je dana ostalo do preuzimanja. Negativno znaci da je termin poceo.</summary>
    public double DanaDoPreuzimanja { get; set; }

    /// <summary>True kad otkazivanje pokrece osoblje - tada je povrat pun.</summary>
    public bool OtkazujeAgencija { get; set; }

    /// <summary>Zbir stvarno naplacenih iznosa. Nula znaci da placanja jos nije bilo.</summary>
    public decimal Naplaceno { get; set; }

    /// <summary>Vec izvrseni ili zapoceti povrati po ovoj rezervaciji.</summary>
    public decimal VecVraceno { get; set; }

    /// <summary>Dio naplacenog koji je depozit - vraca se u cijelosti.</summary>
    public decimal DioDepozita { get; set; }

    /// <summary>Dio naplacenog koji je najam sa opremom i osiguranjem - na njega ide pravilo.</summary>
    public decimal DioNajma { get; set; }

    public decimal ProcenatPovrataNajma { get; set; }
    public decimal PovratNajma { get; set; }
    public decimal PovratDepozita { get; set; }

    /// <summary>Ono sto se stvarno isplacuje: povrat najma i depozita, umanjen za vec vraceno.</summary>
    public decimal UkupanPovrat { get; set; }

    /// <summary>Dio naplacenog koji ostaje agenciji kao naknada za kasno otkazivanje.</summary>
    public decimal ZadrzanoAgenciji { get; set; }

    public string Obrazlozenje { get; set; } = null!;
}
