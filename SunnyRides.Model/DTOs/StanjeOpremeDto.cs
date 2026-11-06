namespace SunnyRides.Model.DTOs;

/// <summary>Koliko komada jedne vrste opreme poslovnica ima na zalihi.</summary>
public class StanjeOpremeDto
{
    public int Id { get; set; }

    public int VrstaOpremeId { get; set; }
    public string? VrstaOpremeNaziv { get; set; }

    public int PoslovnicaId { get; set; }
    public string? PoslovnicaNaziv { get; set; }

    public int Kolicina { get; set; }
}
