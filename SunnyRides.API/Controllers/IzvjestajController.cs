using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Izvjestaji;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Izvjestaji.
///
/// Svaki postoji u dva oblika: kao podatak i kao PDF. Podatak sluzi za pregled prije
/// generisanja - uputstvo trazi da korisnik provjeri parametre prije nego dobije
/// dokument - a PDF se gradi iz tog istog podatka, pa pregled i dokument ne mogu
/// pokazivati razlicite brojeve.
///
/// PDF se vraca kao bajtovi. Desktop aplikacija ih preuzima i otvara za pregled i
/// stampu; nista se ne generise u Flutteru.
/// </summary>
[ApiController]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
[Route("api/izvjestaji")]
public class IzvjestajController : ControllerBase
{
    private readonly IIzvjestajService _izvjestajService;

    public IzvjestajController(IIzvjestajService izvjestajService)
    {
        _izvjestajService = izvjestajService;
    }

    [HttpGet("iskoristenost-flote")]
    public async Task<IskoristenostFloteDto> IskoristenostFloteAsync(
        [FromQuery] IzvjestajSearchObject search, CancellationToken ct)
    {
        return await _izvjestajService.IskoristenostFloteAsync(search, ct);
    }

    [HttpGet("iskoristenost-flote/pdf")]
    public async Task<IActionResult> IskoristenostFlotePdfAsync(
        [FromQuery] IzvjestajSearchObject search, CancellationToken ct)
    {
        var sadrzaj = await _izvjestajService.IskoristenostFlotePdfAsync(search, ct);

        return File(sadrzaj, "application/pdf", NazivFajla("iskoristenost-flote", search));
    }

    [HttpGet("finansijski-pregled")]
    public async Task<FinansijskiPregledDto> FinansijskiPregledAsync(
        [FromQuery] IzvjestajSearchObject search, CancellationToken ct)
    {
        return await _izvjestajService.FinansijskiPregledAsync(search, ct);
    }

    [HttpGet("finansijski-pregled/pdf")]
    public async Task<IActionResult> FinansijskiPregledPdfAsync(
        [FromQuery] IzvjestajSearchObject search, CancellationToken ct)
    {
        var sadrzaj = await _izvjestajService.FinansijskiPregledPdfAsync(search, ct);

        return File(sadrzaj, "application/pdf", NazivFajla("finansijski-pregled", search));
    }

    /// <summary>
    /// Naziv fajla nosi period, da preuzeti dokumenti u folderu budu razlucivi. Bez toga
    /// bi svi bili "izvjestaj.pdf" i pisali jedan preko drugog.
    /// </summary>
    private static string NazivFajla(string osnova, IzvjestajSearchObject search)
    {
        var od = search.Od?.ToString("yyyy-MM-dd") ?? "pocetak";
        var doDatuma = search.Do?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        return $"{osnova}-{od}-{doDatuma}.pdf";
    }
}
