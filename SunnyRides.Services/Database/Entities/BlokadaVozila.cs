namespace SunnyRides.Services.Database.Entities;

/// <summary>Period u kojem vozilo nije dostupno za najam. Ulazi u istu provjeru dostupnosti kao i rezervacije.</summary>
public class BlokadaVozila
{
    public int Id { get; set; }
    public int VoziloId { get; set; }
    public DateTime DatumOd { get; set; }
    public DateTime DatumDo { get; set; }
    public string Razlog { get; set; } = null!;
    public int KreiraoKorisnikId { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public Vozilo Vozilo { get; set; } = null!;
    public Korisnik KreiraoKorisnik { get; set; } = null!;
}
