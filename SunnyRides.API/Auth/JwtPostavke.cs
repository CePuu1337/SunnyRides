namespace SunnyRides.API.Auth;

/// <summary>
/// Postavke za potpisivanje i validaciju tokena. Citaju se iz environment varijabli
/// jednom, pri pokretanju, i registruju kao singleton - ne pri svakom pozivu.
/// </summary>
public class JwtPostavke
{
    public required string Kljuc { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int TrajanjeMinuta { get; init; }

    public static JwtPostavke IzOkruzenja()
    {
        var kljuc = Environment.GetEnvironmentVariable("JWT_KEY")
            ?? throw new InvalidOperationException("JWT_KEY nije postavljen.");

        if (kljuc.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT_KEY mora imati najmanje 32 znaka. Kraci kljuc HMAC-SHA256 odbija.");
        }

        return new JwtPostavke
        {
            Kljuc = kljuc,
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "SunnyRides",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "SunnyRidesClients",
            TrajanjeMinuta = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out var m)
                ? m
                : 120
        };
    }
}
