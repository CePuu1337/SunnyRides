namespace SunnyRides.Model.DTOs;

public class PrijavaOdgovorDto
{
    public string Token { get; set; } = null!;

    public DateTime IsticeUtc { get; set; }

    public KorisnikDto Korisnik { get; set; } = null!;
}
