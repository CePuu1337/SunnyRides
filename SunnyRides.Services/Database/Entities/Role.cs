namespace SunnyRides.Services.Database.Entities;

public class Role
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public string? Opis { get; set; }

    public ICollection<KorisnikRole> KorisnikRole { get; set; } = new List<KorisnikRole>();
}
