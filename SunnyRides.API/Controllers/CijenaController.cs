using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Cijene;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Razrada cijene prije nego rezervacija postoji.
///
/// Klijent na ekranu sa detaljima vozila mijenja period, opremu i paket osiguranja
/// i odmah vidi kako se cijena mijenja. Svaki taj prikaz dolazi sa servera - u
/// Flutteru se ne racuna nista, pa nema sanse da se prikazana i naplacena cijena
/// raziduju.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cijene")]
public class CijenaController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public CijenaController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpPost("obracun")]
    public async Task<CijenaRezervacijeDto> ObracunAsync(
        [FromBody] ObracunCijeneRequest request, CancellationToken ct)
    {
        return await _pricingService.IzracunajAsync(
            request.VoziloId, request.DatumOd, request.DatumDo,
            request.Oprema, request.PaketOsiguranjaId, ct);
    }

    /// <summary>
    /// Cijena po satu i po danu i popusti koji vaze za vozilo na dati dan. Ekran sa
    /// detaljima vozila prikazuje ovo, a ne tarifu upisanu na vozilu - u sezoni se one
    /// razlikuju, a klijent mora vidjeti ono sto ce platiti.
    /// </summary>
    [HttpGet("vozilo/{voziloId:int}")]
    public async Task<TarifaNaDanDto> TarifaAsync(
        int voziloId, [FromQuery] DateTime? naDan, CancellationToken ct)
    {
        return await _pricingService.TarifaNaDanAsync(voziloId, naDan ?? DateTime.UtcNow, ct);
    }
}
