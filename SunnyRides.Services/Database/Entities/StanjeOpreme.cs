namespace SunnyRides.Services.Database.Entities;

/// <summary>Zalihe pojedine vrste opreme po poslovnici. Oprema bez zaliha se ne nudi pri rezervaciji.</summary>
public class StanjeOpreme
{
    public int Id { get; set; }
    public int VrstaOpremeId { get; set; }
    public int PoslovnicaId { get; set; }
    public int Kolicina { get; set; }

    public VrstaOpreme VrstaOpreme { get; set; } = null!;
    public Poslovnica Poslovnica { get; set; } = null!;
}
