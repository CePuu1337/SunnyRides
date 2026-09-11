using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.API.Filters;

/// <summary>
/// Jedino mjesto gdje se odlucuje koji HTTP status klijent dobija. Servisi bacaju
/// izuzetke i ne znaju nista o HTTP-u.
///
/// Klijent nikad ne dobija stack trace ni internu poruku iz baze - neocekivana
/// greska se logira u cijelosti na serveru, a napolje ide standardizovana poruka.
/// </summary>
public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var (status, poruka, naslov) = context.Exception switch
        {
            BusinessException ex =>
                (StatusCodes.Status400BadRequest, ex.Message, "Zahtjev nije prihvacen"),

            NotFoundException ex =>
                (StatusCodes.Status404NotFound, ex.Message, "Zapis nije pronadjen"),

            ForbiddenException ex =>
                (StatusCodes.Status403Forbidden, ex.Message, "Pristup nije dozvoljen"),

            _ => (StatusCodes.Status500InternalServerError,
                  "Doslo je do neocekivane greske. Pokusajte ponovo, a ako se ponovi, obratite se podrsci.",
                  "Greska na serveru")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Neocekivana greska - logira se sa punim kontekstom, jer je jedini
            // trag koji ostaje kad aplikacija padne pri pregledu rada.
            _logger.LogError(context.Exception,
                "Neobradjena greska na {Metoda} {Putanja}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);
        }
        else
        {
            // Ocekivano odbijanje zahtjeva - nije greska sistema, ali se biljezi.
            _logger.LogWarning(
                "Zahtjev odbijen ({Status}) na {Metoda} {Putanja}: {Poruka}",
                status,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                context.Exception.Message);
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = naslov,
            Detail = poruka,
            Instance = context.HttpContext.Request.Path
        })
        {
            StatusCode = status
        };

        context.ExceptionHandled = true;
    }
}
