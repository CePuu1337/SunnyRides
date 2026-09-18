using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Preporuke;
using SunnyRides.Services.Preporuke.Ml;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Preporuke vozila.
///
/// Endpoint je namijenjen klijentu i sve izvodi iz njegovog tokena - ko pita, sta
/// smije voziti i sta je ranije trazio. Osoblje nema vozacku dozvolu u sistemu, pa
/// za njih preporuka nema ni sadrzaja ni smisla.
/// </summary>
[ApiController]
[Authorize]
[Route("api/preporuke")]
public class PreporukaController : ControllerBase
{
    private readonly IRecommenderService _recommenderService;
    private readonly IModelPreporuke _model;

    public PreporukaController(IRecommenderService recommenderService, IModelPreporuke model)
    {
        _recommenderService = recommenderService;
        _model = model;
    }

    /// <summary>
    /// Preporuke za prijavljenog korisnika, poredane po skoru.
    ///
    /// Odgovor je paginiran kao i svaka druga lista, s tim da je ovdje gornja granica
    /// stroza - dvadeset umjesto sto. Preporuka koja je dvadeseta po redu vise nije
    /// preporuka nego katalog.
    /// </summary>
    [HttpGet]
    public async Task<PagedResult<PreporukaDto>> GetAsync(
        [FromQuery] PreporukaSearchObject search, CancellationToken ct)
    {
        return await _recommenderService.PreporuciAsync(search, ct);
    }

    /// <summary>
    /// Vozila slicna zadatom, za ekran sa detaljima vozila. Isti racun slicnosti, samo
    /// sto se polaziste uzima iz vozila koje korisnik gleda, a ne iz njegove historije.
    /// </summary>
    [HttpGet("slicna/{voziloId:int}")]
    public async Task<List<PreporukaDto>> SlicnaAsync(
        int voziloId, [FromQuery] int? broj, CancellationToken ct)
    {
        return await _recommenderService.SlicnaVozilaAsync(voziloId, broj, ct);
    }

    /// <summary>
    /// Stanje istreniranog modela: na koliko podataka je ucen, koliko ima korisnika i
    /// modela vozila, te kolika mu je greska na ocjenama koje pri ucenju nije vidio.
    /// </summary>
    [HttpGet("model")]
    [Authorize(Roles = Uloge.Administrator)]
    public StanjeModelaDto StanjeModela()
    {
        return _model.Stanje;
    }

    /// <summary>
    /// Treniranje na zahtjev. Model se inace sam osvjezava, ali poslije unosa novih
    /// ocjena je korisno vidjeti rezultat odmah.
    /// </summary>
    [HttpPost("model/treniraj")]
    [Authorize(Roles = Uloge.Administrator)]
    public async Task<StanjeModelaDto> TrenirajAsync(CancellationToken ct)
    {
        return await _model.TrenirajAsync(ct);
    }
}
