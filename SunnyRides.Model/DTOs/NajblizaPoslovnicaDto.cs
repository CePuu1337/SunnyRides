namespace SunnyRides.Model.DTOs;

/// <summary>Poslovnica najbliza klijentu, za karticu na pocetnom ekranu.</summary>
public class NajblizaPoslovnicaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public string Adresa { get; set; } = null!;
    public string? GradNaziv { get; set; }
    public string? RadnoVrijeme { get; set; }

    /// <summary>Zracna udaljenost u kilometrima, zaokruzena na jednu decimalu.</summary>
    public double UdaljenostKm { get; set; }

    /// <summary>
    /// Udaljenost je racunata od trenutne lokacije telefona. Kad nije, racunata je od
    /// grada iz profila, ciji naziv stoji u <see cref="PolazniGrad"/>. Aplikacija to
    /// pise uz broj, da korisnik zna od cega je mjereno.
    /// </summary>
    public bool PoLokaciji { get; set; }
    public string? PolazniGrad { get; set; }
}
