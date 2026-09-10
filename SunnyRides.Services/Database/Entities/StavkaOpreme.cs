namespace SunnyRides.Services.Database.Entities;

/// <summary>Podredjeni zapis master-details forme. Cijena se snima u trenutku kreiranja.</summary>
public class StavkaOpreme
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public int VrstaOpremeId { get; set; }
    public int Kolicina { get; set; }
    public decimal CijenaPoJedinici { get; set; }
    public decimal Iznos { get; set; }

    public Rezervacija Rezervacija { get; set; } = null!;
    public VrstaOpreme VrstaOpreme { get; set; } = null!;
}
