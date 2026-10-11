using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunnyRides.API.Auth;
using SunnyRides.Services.Database;

namespace SunnyRides.API.Middleware;

/// <summary>
/// Provjerava je li token jos vazeci, i onda kad mu rok nije istekao.
///
/// JWT je po prirodi bez stanja - jednom potpisan, vazi do isteka roka. Zato se svaki
/// autentifikovan zahtjev jednim upitom poredi sa bazom, i odbija se token:
///
/// <list type="bullet">
/// <item>koji je opozvan odjavom (tabela <c>OpozvaniToken</c>, indeks na Jti; worker
/// iz nje brise zapise kojima je rok ionako istekao),</item>
/// <item>ciji je nalog u medjuvremenu deaktiviran,</item>
/// <item>cija verzija vise nije ona iz baze - lozinka je promijenjena ili su uloge
/// izmijenjene, pa stari token ne smije nositi stara prava.</item>
/// </list>
/// </summary>
public class OpozvaniTokenMiddleware
{
    private readonly RequestDelegate _sljedeci;
    private readonly ILogger<OpozvaniTokenMiddleware> _logger;

    public OpozvaniTokenMiddleware(RequestDelegate sljedeci, ILogger<OpozvaniTokenMiddleware> logger)
    {
        _sljedeci = sljedeci;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, SunnyRidesDbContext dbContext)
    {
        // Stripe webhook se prijavljuje potpisom, ne tokenom korisnika, pa nema ni sub
        // ni jti - za njega ova provjera ne vazi.
        var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (context.User.Identity?.IsAuthenticated == true
            && !string.IsNullOrWhiteSpace(jti)
            && int.TryParse(sub, out var korisnikId))
        {
            // Token izdat prije uvodjenja verzije nema taj claim; vrijedi kao verzija 0.
            var verzijaUTokenu = int.TryParse(
                context.User.FindFirst(TokenService.ClaimVerzije)?.Value, out var verzija) ? verzija : 0;

            var stanje = await dbContext.Korisnici
                .Where(x => x.Id == korisnikId)
                .Select(x => new
                {
                    x.Aktivan,
                    x.VerzijaTokena,
                    Opozvan = dbContext.OpozvaniTokeni.Any(o => o.Jti == jti)
                })
                .FirstOrDefaultAsync(context.RequestAborted);

            var razlog = stanje switch
            {
                null => "Nalog vise ne postoji.",
                { Opozvan: true } => "Token je ponisten odjavom. Prijavite se ponovo.",
                { Aktivan: false } => "Nalog je deaktiviran. Obratite se agenciji.",
                { } s when s.VerzijaTokena != verzijaUTokenu =>
                    "Lozinka ili prava pristupa su promijenjeni. Prijavite se ponovo.",
                _ => null
            };

            if (razlog is not null)
            {
                _logger.LogWarning(
                    "Odbijen token {Jti} korisnika {KorisnikId} na {Metoda} {Putanja}: {Razlog}",
                    jti, korisnikId, context.Request.Method, context.Request.Path, razlog);

                await OdbijAsync(context, razlog);
                return;
            }
        }

        await _sljedeci(context);
    }

    private static async Task OdbijAsync(HttpContext context, string razlog)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Sesija je zavrsena",
            Detail = razlog,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem), context.RequestAborted);
    }
}
