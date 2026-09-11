using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SunnyRides.Services.Auth;

namespace SunnyRides.API.Auth;

public class TokenService : ITokenService
{
    static TokenService()
    {
        // Bez ovoga bi se kratki nazivi claimova prevodili u duge URI oblike,
        // pa se ono sto se upise u token ne bi poklapalo sa onim sto se cita.
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
    }

    private readonly JwtPostavke _postavke;

    public TokenService(JwtPostavke postavke)
    {
        _postavke = postavke;
    }

    public GenerisaniToken Generisi(int korisnikId, string korisnickoIme, string ime, string prezime,
        IEnumerable<string> uloge)
    {
        var jti = Guid.NewGuid().ToString("N");
        var izdat = DateTime.UtcNow;
        var istice = izdat.AddMinutes(_postavke.TrajanjeMinuta);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, korisnikId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Name, korisnickoIme),
            new("ime", ime),
            new("prezime", prezime)
        };

        claims.AddRange(uloge.Select(uloga => new Claim("role", uloga)));

        var kljuc = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_postavke.Kljuc));
        var potpis = new SigningCredentials(kljuc, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _postavke.Issuer,
            audience: _postavke.Audience,
            claims: claims,
            notBefore: izdat,
            expires: istice,
            signingCredentials: potpis);

        return new GenerisaniToken(new JwtSecurityTokenHandler().WriteToken(token), istice, jti);
    }
}
