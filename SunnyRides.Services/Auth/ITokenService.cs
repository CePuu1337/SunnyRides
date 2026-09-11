namespace SunnyRides.Services.Auth;

/// <summary>Generisani JWT, zajedno sa podacima koji trebaju pri odjavi.</summary>
public record GenerisaniToken(string Token, DateTime IsticeUtc, string Jti);

/// <summary>
/// Generisanje JWT tokena. Interfejs zivi u servisnom sloju, a implementacija u API
/// projektu - jer je JWT transportna stvar i tamo je vec konfigurisana validacija.
/// Servisni sloj time ostaje bez zavisnosti prema ASP.NET Core-u.
/// </summary>
public interface ITokenService
{
    GenerisaniToken Generisi(int korisnikId, string korisnickoIme, string ime, string prezime,
        IEnumerable<string> uloge);
}
