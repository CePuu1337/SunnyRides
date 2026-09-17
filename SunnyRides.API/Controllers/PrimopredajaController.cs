using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Fajlovi;
using SunnyRides.Services.Primopredaje;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Izdavanje i povrat vozila.
///
/// Upis rade samo uposlenik i administrator. Klijent smije citati primopredaje i
/// fotografije svojih rezervacija - servis tu provjerava vlasnistvo prema tokenu.
/// </summary>
[Route("api/primopredaje")]
public class PrimopredajaController : BaseController<PrimopredajaDto, PrimopredajaSearchObject>
{
    private const long NajvecaVelicinaZahtjeva = 60 * 1024 * 1024;

    private readonly IPrimopredajaService _primopredajaService;

    public PrimopredajaController(IPrimopredajaService primopredajaService)
        : base(primopredajaService)
    {
        _primopredajaService = primopredajaService;
    }

    /// <summary>Izdavanje vozila. Podaci i fotografije stizu zajedno, kao multipart forma.</summary>
    [HttpPost("izdavanje")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    [RequestSizeLimit(NajvecaVelicinaZahtjeva)]
    public async Task<PrimopredajaDto> IzdajAsync(
        [FromForm] IzdavanjeVozilaRequest request, List<IFormFile>? fotografije, CancellationToken ct)
    {
        return await SaFotografijamaAsync(fotografije,
            ulaz => _primopredajaService.IzdajAsync(request, ulaz, ct));
    }

    /// <summary>
    /// Povrat vozila. Fotografije idu u istom zahtjevu, jer je bar jedna obavezna kad
    /// je oznaceno ostecenje - odvojen upload bi to pravilo mogao zaobici.
    /// </summary>
    [HttpPost("povrat")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    [RequestSizeLimit(NajvecaVelicinaZahtjeva)]
    public async Task<PrimopredajaDto> VratiAsync(
        [FromForm] PovratVozilaRequest request, List<IFormFile>? fotografije, CancellationToken ct)
    {
        return await SaFotografijamaAsync(fotografije,
            ulaz => _primopredajaService.VratiAsync(request, ulaz, ct));
    }

    /// <summary>Obracun depozita za formu povrata. Nista ne upisuje.</summary>
    [HttpGet("obracun-povrata/{rezervacijaId:int}")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<ObracunPovrataDto> ObracunPovrataAsync(
        int rezervacijaId, [FromQuery] DateTime? datumPovrata, [FromQuery] decimal? iznosStete, CancellationToken ct)
    {
        return await _primopredajaService.ObracunPovrataAsync(rezervacijaId, datumPovrata, iznosStete, ct);
    }

    /// <summary>Preuzimanja i vracanja zakazana za dan.</summary>
    [HttpGet("raspored")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<PagedResult<RasporedStavkaDto>> RasporedAsync(
        [FromQuery] RasporedSearchObject search, CancellationToken ct)
    {
        return await _primopredajaService.RasporedAsync(search, ct);
    }

    [HttpGet("fotografije/{fotografijaId:int}")]
    public async Task<IActionResult> PreuzmiFotografijuAsync(int fotografijaId, CancellationToken ct)
    {
        var fajl = await _primopredajaService.PreuzmiFotografijuAsync(fotografijaId, ct);

        return File(fajl.Sadrzaj, fajl.ContentType, fajl.NazivFajla);
    }

    /// <summary>Otvara streamove otpremljenih fajlova i zatvara ih kad servis zavrsi.</summary>
    private static async Task<PrimopredajaDto> SaFotografijamaAsync(
        List<IFormFile>? fotografije, Func<IReadOnlyList<UlazniFajl>, Task<PrimopredajaDto>> posao)
    {
        var streamovi = new List<Stream>();

        try
        {
            var ulaz = new List<UlazniFajl>();

            foreach (var fajl in (fotografije ?? new List<IFormFile>()).Where(f => f.Length > 0))
            {
                var stream = fajl.OpenReadStream();
                streamovi.Add(stream);
                ulaz.Add(new UlazniFajl(stream, fajl.Length));
            }

            return await posao(ulaz);
        }
        finally
        {
            foreach (var stream in streamovi)
            {
                await stream.DisposeAsync();
            }
        }
    }
}
