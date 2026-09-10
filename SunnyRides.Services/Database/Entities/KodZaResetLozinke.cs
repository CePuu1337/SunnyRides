namespace SunnyRides.Services.Database.Entities;

/// <summary>Kod za reset lozinke. Cuva se hashiran i ima definisan rok isteka.</summary>
public class KodZaResetLozinke
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public string KodHash { get; set; } = null!;
    public DateTime DatumIsteka { get; set; }
    public bool Iskoristen { get; set; }
    public DateTime DatumKreiranja { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
}
