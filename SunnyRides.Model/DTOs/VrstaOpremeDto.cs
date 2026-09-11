namespace SunnyRides.Model.DTOs;

public class VrstaOpremeDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;

    /// <summary>Postavljena je tacno jedna od ove dvije cijene - nikad obje, nikad nijedna.</summary>
    public decimal? CijenaPoDanu { get; set; }
    public decimal? FiksnaCijena { get; set; }
}
