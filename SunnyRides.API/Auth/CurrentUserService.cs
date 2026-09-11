using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.API.Auth;

/// <summary>
/// Cita podatke o prijavljenom korisniku iskljucivo iz JWT tokena. Nijedna vrijednost
/// ne dolazi iz rute, query stringa ni tijela zahtjeva.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Korisnik => _accessor.HttpContext?.User;

    public int? KorisnikId =>
        int.TryParse(Korisnik?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public string? KorisnickoIme => Korisnik?.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

    public string? Jti => Korisnik?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    public DateTime? IsticeUtc =>
        long.TryParse(Korisnik?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value, out var sekunde)
            ? DateTimeOffset.FromUnixTimeSeconds(sekunde).UtcDateTime
            : null;

    public IReadOnlyList<string> Uloge =>
        Korisnik?.FindAll("role").Select(x => x.Value).ToList() ?? new List<string>();

    public bool JeUUlozi(string uloga) =>
        Uloge.Contains(uloga, StringComparer.OrdinalIgnoreCase);

    public int ObaveznoKorisnikId() =>
        KorisnikId ?? throw new ForbiddenException("Zahtjev nije autentifikovan.");
}
