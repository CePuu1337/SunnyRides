using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Ugovor o najmu jednog vozila u odredjenom periodu. Nikad se ne brise - otkazivanje je promjena statusa.</summary>
public class Rezervacija
{
    public int Id { get; set; }
    public string Broj { get; set; } = null!;
    public int KorisnikId { get; set; }
    public int VoziloId { get; set; }
    public int PoslovnicaId { get; set; }
    public int? PaketOsiguranjaId { get; set; }
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public StatusRezervacije Status { get; set; } = StatusRezervacije.Pending;
    public decimal UkupanIznos { get; set; }
    public decimal IznosDepozita { get; set; }
    public decimal IznosPopusta { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? DrziDo { get; set; }
    public DateTime DatumKreiranja { get; set; }
    public int? RazlogOtkazivanjaId { get; set; }
    public string? NapomenaOtkazivanja { get; set; }
    public int? OtkazaoKorisnikId { get; set; }
    public DateTime? DatumOtkazivanja { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public Vozilo Vozilo { get; set; } = null!;
    public Poslovnica Poslovnica { get; set; } = null!;
    public PaketOsiguranja? PaketOsiguranja { get; set; }
    public Korisnik? OtkazaoKorisnik { get; set; }
    public RazlogOtkazivanja? RazlogOtkazivanja { get; set; }
    public ICollection<StavkaOpreme> StavkeOpreme { get; set; } = new List<StavkaOpreme>();
    public ICollection<Placanje> Placanja { get; set; } = new List<Placanje>();
    public ICollection<Primopredaja> Primopredaje { get; set; } = new List<Primopredaja>();
    public ICollection<HistorijaStatusaRezervacije> HistorijaStatusa { get; set; } = new List<HistorijaStatusaRezervacije>();
    public Recenzija? Recenzija { get; set; }
}
