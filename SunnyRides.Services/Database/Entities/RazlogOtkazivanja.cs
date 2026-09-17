namespace SunnyRides.Services.Database.Entities;

/// <summary>
/// Unaprijed zadan razlog otkazivanja koji se bira iz padajuce liste.
/// Klijent i agencija vide razlicite razloge, a "Ostalo" trazi kratko objasnjenje.
/// </summary>
public class RazlogOtkazivanja
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public bool ZaKlijenta { get; set; }
    public bool ZaAgenciju { get; set; }
    public bool TraziNapomenu { get; set; }
    public bool Aktivan { get; set; } = true;

    public ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
}
