namespace SunnyRides.Model.DTOs;

/// <summary>
/// Podredjeni zapis master-details forme. Cijena je ona koja je vazila u trenutku
/// kreiranja - kasnija izmjena cjenovnika ne mijenja historijsku rezervaciju.
/// </summary>
public class StavkaOpremeDto
{
    public int Id { get; set; }
    public int VrstaOpremeId { get; set; }
    public string? Naziv { get; set; }
    public int Kolicina { get; set; }
    public decimal CijenaPoJedinici { get; set; }
    public decimal Iznos { get; set; }
}
