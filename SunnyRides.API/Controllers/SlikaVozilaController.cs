using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Flota;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Galerija jednog vozila, kao pod-resurs vozila.
///
/// Lista vozila namjerno ne vraca galeriju nego samo URL thumbnaila - ovaj endpoint
/// postoji da ekran sa detaljima dohvati ostale fotografije kad mu zatrebaju.
/// </summary>
[ApiController]
[Authorize]
[Route("api/vozila/{voziloId:int}/slike")]
public class SlikaVozilaController : ControllerBase
{
    private readonly ISlikaVozilaService _service;

    public SlikaVozilaController(ISlikaVozilaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<List<SlikaVozilaDto>> GetAsync(int voziloId, CancellationToken ct)
    {
        return await _service.ZaVoziloAsync(voziloId, ct);
    }

    /// <summary>
    /// Otprema fotografije. Sadrzaj ide kao multipart, nikad kao base64 u JSON-u -
    /// base64 povecava prenos za trecinu i cijeli fajl drzi u memoriji kao string.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<SlikaVozilaDto> UploadAsync(
        int voziloId, IFormFile fajl, CancellationToken ct)
    {
        if (fajl is null || fajl.Length == 0)
        {
            throw new BusinessException("Odaberite fotografiju.");
        }

        // Kontroler ne otvara fajl, ne provjerava format i ne racuna putanju - samo
        // prosljedjuje sadrzaj servisu. Sve odluke o tome sta je ispravna slika i
        // gdje zavrsava su u servisnom sloju.
        await using var sadrzaj = fajl.OpenReadStream();

        return await _service.DodajAsync(voziloId, sadrzaj, fajl.Length, ct);
    }

    [HttpPut("{slikaId:int}/glavna")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<SlikaVozilaDto> PostaviGlavnuAsync(
        int voziloId, int slikaId, CancellationToken ct)
    {
        return await _service.PostaviGlavnuAsync(voziloId, slikaId, ct);
    }

    [HttpDelete("{slikaId:int}")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<IActionResult> DeleteAsync(int voziloId, int slikaId, CancellationToken ct)
    {
        await _service.ObrisiAsync(voziloId, slikaId, ct);
        return NoContent();
    }
}
