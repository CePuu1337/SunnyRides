namespace SunnyRides.Model.DTOs;

public class GradDto
{
    public int Id { get; set; }
    public int DrzavaId { get; set; }
    public string Naziv { get; set; } = null!;
    public string? PostanskiBroj { get; set; }

    /// <summary>Naziv drzave, da lista ne mora za svaki red praviti jos jedan poziv.</summary>
    public string? DrzavaNaziv { get; set; }
}
