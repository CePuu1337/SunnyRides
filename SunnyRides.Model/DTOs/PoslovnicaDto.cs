namespace SunnyRides.Model.DTOs;

public class PoslovnicaDto
{
    public int Id { get; set; }
    public int GradId { get; set; }
    public string Naziv { get; set; } = null!;
    public string Adresa { get; set; } = null!;
    public double? Latituda { get; set; }
    public double? Longituda { get; set; }
    public string? RadnoVrijeme { get; set; }

    public string? GradNaziv { get; set; }
    public string? GradDrzavaNaziv { get; set; }
}
