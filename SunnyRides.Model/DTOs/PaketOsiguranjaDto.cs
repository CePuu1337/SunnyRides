namespace SunnyRides.Model.DTOs;

public class PaketOsiguranjaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public decimal CijenaPoDanu { get; set; }
    public decimal IznosUcesca { get; set; }
}
