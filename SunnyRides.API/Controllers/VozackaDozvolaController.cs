using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Dozvole;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Vozacke dozvole. Kontroler ima dvije jasno odvojene strane.
///
/// Klijent radi iskljucivo sa **svojom** dozvolom i nikad ne navodi ciju - vlasnik se
/// cita iz tokena. Zato rute za klijenta nemaju identifikator u putanji; da ga imaju,
/// bilo bi dovoljno promijeniti broj u URL-u da se vidi tudja dozvola.
///
/// Uposlenik radi po identifikatoru, jer verifikuje tudje dozvole - i te rute traze
/// ulogu.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dozvole")]
public class VozackaDozvolaController : ControllerBase
{
    private readonly IDozvolaService _dozvolaService;
    private readonly ICurrentUserService _trenutniKorisnik;

    public VozackaDozvolaController(
        IDozvolaService dozvolaService, ICurrentUserService trenutniKorisnik)
    {
        _dozvolaService = dozvolaService;
        _trenutniKorisnik = trenutniKorisnik;
    }

    // --- klijent -----------------------------------------------------------

    /// <summary>Dozvola prijavljenog korisnika. Vraca 204 ako je jos nije prijavio.</summary>
    [HttpGet("moja")]
    public async Task<IActionResult> MojaAsync(CancellationToken ct)
    {
        var dozvola = await _dozvolaService.MojaAsync(ct);

        return dozvola is null ? NoContent() : Ok(dozvola);
    }

    /// <summary>Prijava ili izmjena vlastite dozvole. Uvijek vraca status na cekanje.</summary>
    [HttpPost]
    public async Task<VozackaDozvolaDto> PrijaviAsync(
        [FromBody] VozackaDozvolaRequest request, CancellationToken ct)
    {
        return await _dozvolaService.PrijaviAsync(request, ct);
    }

    /// <summary>Sta prijavljeni korisnik smije voziti, sa obrazlozenjem za prikaz iznad pretrage.</summary>
    [HttpGet("moje-kategorije")]
    public async Task<DozvoljeneKategorijeDto> MojeKategorijeAsync(
        [FromQuery] DateTime? naDan, CancellationToken ct)
    {
        return await _dozvolaService.DozvoljeneKategorijeAsync(
            _trenutniKorisnik.ObaveznoKorisnikId(), naDan, ct);
    }

    // --- osoblje -----------------------------------------------------------

    [HttpGet]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<PagedResult<VozackaDozvolaDto>> GetAsync(
        [FromQuery] VozackaDozvolaSearchObject search, CancellationToken ct)
    {
        return await _dozvolaService.GetAsync(search, ct);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<VozackaDozvolaDto> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _dozvolaService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Sta bi klijent smio voziti kad bi mu dozvola bila odobrena.
    ///
    /// Uposlenik ovo vidi na ekranu za verifikaciju - uputstvo trazi da sistem ispise
    /// koja vozila klijent smije voziti, izvedeno iz kategorija i pravila, umjesto da
    /// uposlenik to zakljucuje sam.
    /// </summary>
    [HttpGet("{id:int}/dozvoljene-kategorije")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<DozvoljeneKategorijeDto> DozvoljeneKategorijeAsync(
        int id, CancellationToken ct)
    {
        return await _dozvolaService.PokrivenostZaDozvoluAsync(id, ct);
    }

    [HttpPost("{id:int}/odobri")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<VozackaDozvolaDto> OdobriAsync(int id, CancellationToken ct)
    {
        return await _dozvolaService.OdobriAsync(id, ct);
    }

    [HttpPost("{id:int}/odbij")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<VozackaDozvolaDto> OdbijAsync(
        int id, [FromBody] OdbijDozvoluRequest request, CancellationToken ct)
    {
        return await _dozvolaService.OdbijAsync(id, request, ct);
    }
}
