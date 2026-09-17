using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Fizicko izdavanje ili vracanje vozila. NivoGoriva je procenat 0-100.</summary>
public class Primopredaja
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public TipPrimopredaje Tip { get; set; }
    public DateTime DatumVrijeme { get; set; }
    public int Kilometraza { get; set; }
    public int NivoGoriva { get; set; }
    public bool KontrolnaListaProdjena { get; set; }
    public string? Napomena { get; set; }
    public int IzvrsioKorisnikId { get; set; }

    public Rezervacija Rezervacija { get; set; } = null!;
    public Korisnik IzvrsioKorisnik { get; set; } = null!;
    public ICollection<FotografijaPrimopredaje> Fotografije { get; set; } = new List<FotografijaPrimopredaje>();
    public EvidencijaStete? EvidencijaStete { get; set; }
}
