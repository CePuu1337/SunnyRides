namespace SunnyRides.Services.Database.Entities;

public class Grad
{
    public int Id { get; set; }
    public int DrzavaId { get; set; }
    public string Naziv { get; set; } = null!;
    public string? PostanskiBroj { get; set; }

    /// <summary>
    /// Sredina grada. Iz nje se racuna udaljenost do poslovnica za klijenta koji je
    /// ovdje, i kad grad sam nema poslovnicu.
    /// </summary>
    public double? Latituda { get; set; }
    public double? Longituda { get; set; }

    public Drzava Drzava { get; set; } = null!;
    public ICollection<Poslovnica> Poslovnice { get; set; } = new List<Poslovnica>();
}
