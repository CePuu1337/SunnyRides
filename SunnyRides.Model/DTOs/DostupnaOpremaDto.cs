namespace SunnyRides.Model.DTOs;

/// <summary>
/// Oprema koju klijent moze dodati uz rezervaciju u konkretnom terminu.
///
/// Slobodno je ono sto ostane od zaliha poslovnice kad se oduzme oprema vec
/// obecana drugim rezervacijama u istom terminu. Opreme bez slobodnih komada u
/// listi nema.
/// </summary>
public class DostupnaOpremaDto
{
    /// <summary>Identifikator vrste opreme, isti koji ide u zahtjev za rezervaciju.</summary>
    public int Id { get; set; }

    public string Naziv { get; set; } = null!;

    public decimal? CijenaPoDanu { get; set; }
    public decimal? FiksnaCijena { get; set; }

    public int Slobodno { get; set; }
}
