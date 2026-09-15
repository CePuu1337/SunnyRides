namespace SunnyRides.Model.DTOs;

/// <summary>Jedna stavka u razradi cijene - komad opreme sa svojom cijenom i iznosom.</summary>
public class StavkaCijeneDto
{
    public int VrstaOpremeId { get; set; }
    public string Naziv { get; set; } = null!;
    public int Kolicina { get; set; }

    /// <summary>Cijena po jedinici u trenutku obracuna. Upisuje se na rezervaciju i vise se ne mijenja.</summary>
    public decimal CijenaPoJedinici { get; set; }

    public decimal Iznos { get; set; }
}
