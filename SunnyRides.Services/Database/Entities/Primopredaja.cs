using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Fizicko izdavanje ili vracanje vozila. NivoGoriva je procenat 0-100.</summary>
public class Primopredaja
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public TipPrimopredaje Tip { get; set; }
    /// <summary>Kad se primopredaja stvarno desila.</summary>
    public DateTime DatumVrijeme { get; set; }

    /// <summary>
    /// Kad je zapis unesen u sistem. Obicno isto sto i <see cref="DatumVrijeme"/>, ali
    /// se razlikuje kad uposlenik naknadno evidentira primopredaju koja se desila ranije.
    ///
    /// Dva polja a ne jedno, jer o vremenu dogadjaja ovisi novac - kasnjenje pri povratu
    /// umanjuje depozit - a o vremenu unosa ovisi trag ko je i kada sta upisao. Kad bi
    /// postojalo samo jedno, naknadni unos bi ili falsifikovao trenutak unosa ili
    /// naplatio kasnjenje koje se nije desilo.
    /// </summary>
    public DateTime DatumUnosa { get; set; }
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
