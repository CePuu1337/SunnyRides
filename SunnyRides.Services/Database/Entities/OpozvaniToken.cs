namespace SunnyRides.Services.Database.Entities;

/// <summary>JWT token invalidiran odjavom prije isteka roka. Middleware provjerava svaki zahtjev prema ovoj tabeli.</summary>
public class OpozvaniToken
{
    public int Id { get; set; }
    public string Jti { get; set; } = null!;
    public DateTime DatumIsteka { get; set; }
    public DateTime DatumOpoziva { get; set; }
}
