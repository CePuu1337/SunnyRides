namespace SunnyRides.Services.Database.Entities;

/// <summary>Zapis o jednoj pretrazi. Ulazni podatak za sistem preporuke - upisuje se pri svakoj pretrazi.</summary>
public class HistorijaPretrage
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public int? TipVozilaId { get; set; }
    public int? GradId { get; set; }
    public int? MarkaId { get; set; }
    public decimal? CijenaOd { get; set; }
    public decimal? CijenaDo { get; set; }
    public DateTime DatumVrijeme { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public TipVozila? TipVozila { get; set; }
    public Grad? Grad { get; set; }
    public Marka? Marka { get; set; }
}
