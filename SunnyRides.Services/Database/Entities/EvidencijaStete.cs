namespace SunnyRides.Services.Database.Entities;

/// <summary>Ostecenje uoceno pri povratu vozila. Iznos umanjuje povrat depozita.</summary>
public class EvidencijaStete
{
    public int Id { get; set; }
    public int PrimopredajaId { get; set; }
    public string Opis { get; set; } = null!;
    public decimal Iznos { get; set; }
    public DateTime DatumEvidentiranja { get; set; }
    public int EvidentiraoKorisnikId { get; set; }

    public Primopredaja Primopredaja { get; set; } = null!;
    public Korisnik EvidentiraoKorisnik { get; set; } = null!;
}
