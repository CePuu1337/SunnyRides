namespace SunnyRides.Services.Database.Entities;

/// <summary>Konkretan primjerak u floti - jedno fizicko vozilo sa vlastitom registracijom.</summary>
public class Vozilo
{
    public int Id { get; set; }
    public int ModelVozilaId { get; set; }
    public int PoslovnicaId { get; set; }
    public string RegistarskaOznaka { get; set; } = null!;
    public int GodinaProizvodnje { get; set; }
    public int Kilometraza { get; set; }
    public bool Aktivno { get; set; } = true;
    public decimal DnevnaTarifa { get; set; }
    public decimal SatnaTarifa { get; set; }
    public decimal IznosDepozita { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public ModelVozila ModelVozila { get; set; } = null!;
    public Poslovnica Poslovnica { get; set; } = null!;
    public ICollection<SlikaVozila> Slike { get; set; } = new List<SlikaVozila>();
    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public ICollection<BlokadaVozila> Blokade { get; set; } = new List<BlokadaVozila>();
    public ICollection<Recenzija> Recenzije { get; set; } = new List<Recenzija>();
}
