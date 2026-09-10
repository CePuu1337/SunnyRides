namespace SunnyRides.Services.Database.Entities;

public class KorisnikRole
{
    public int Id { get; set; }
    public int KorisnikId { get; set; }
    public int RoleId { get; set; }
    public DateTime DatumDodjele { get; set; }

    public Korisnik Korisnik { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
