namespace SunnyRides.Model.DTOs;

public class TipGorivaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;

    /// <summary>Vozilo se puni strujom. Aplikacija po ovome pise "baterija" umjesto "gorivo".</summary>
    public bool JeElektricni { get; set; }
}
