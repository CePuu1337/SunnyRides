using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database;

namespace SunnyRides.API.Middleware;

/// <summary>
/// Provjerava je li token opozvan odjavom.
///
/// JWT je po prirodi bez stanja - jednom potpisan, vazi do isteka roka. Uputstvo
/// trazi da odjava invalidira token na serveru, pa se svaki autentifikovan zahtjev
/// poredi sa tabelom opozvanih tokena. Tabela ima indeks na Jti, a periodicni posao
/// u workeru iz nje brise zapise kojima je rok ionako istekao.
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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrWhiteSpace(jti))
            {
                var opozvan = await dbContext.OpozvaniTokeni
                    .AnyAsync(x => x.Jti == jti, context.RequestAborted);

                if (opozvan)
                {
                    _logger.LogWarning(
                        "Odbijen opozvan token {Jti} na {Metoda} {Putanja}",
                        jti, context.Request.Method, context.Request.Path);

                    await OdbijAsync(context);
                    return;
                }
            }
        }

        await _sljedeci(context);
    }

    private static async Task OdbijAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Sesija je zavrsena",
            Detail = "Token je ponisten odjavom. Prijavite se ponovo.",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem), context.RequestAborted);
    }
}
