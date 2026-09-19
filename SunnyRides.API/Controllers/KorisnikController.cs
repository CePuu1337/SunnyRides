using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Korisnici;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Upravljanje nalozima.
///
/// Cijeli kontroler je administratorski. Uposlenik ne upravlja nalozima, a klijent nad
/// svojim nalogom radi kroz <c>/api/profil</c>, gdje identifikator ne postoji - cita se
/// iz tokena.
///
/// Uloga na klasi se ne moze olabaviti atributom na metodi: ASP.NET Core ih ne zamjenjuje
/// nego zahtijeva da prodju oba. Zato pretraga klijenata, koju smije i uposlenik, ima
/// svoj kontroler - <see cref="KlijentController"/>.
/// </summary>
[Route("api/korisnici")]
[Authorize(Roles = Uloge.Administrator)]
public class KorisnikController
    : BaseCRUDController<KorisnikDto, KorisnikSearchObject, KorisnikInsertRequest, KorisnikUpdateRequest>
{
    private readonly IKorisnikService _korisnikService;

    public KorisnikController(IKorisnikService korisnikService)
        : base(korisnikService)
    {
        _korisnikService = korisnikService;
    }

    /// <summary>Uloge za padajucu listu na formi. Uloge se ne unose kroz aplikaciju.</summary>
    [HttpGet("uloge")]
    public async Task<List<RoleDto>> UlogeAsync(CancellationToken ct)
    {
        return await _korisnikService.UlogeAsync(ct);
    }

    [HttpPut("{id:int}/uloge")]
    public async Task<KorisnikDto> PostaviUlogeAsync(
        int id, [FromBody] UlogeKorisnikaRequest request, CancellationToken ct)
    {
        return await _korisnikService.PostaviUlogeAsync(id, request, ct);
    }

    /// <summary>
    /// Administratorski reset lozinke. Ne trazi staru lozinku - administrator je ne zna.
    /// Promjena vlastite lozinke je druga radnja i ide kroz <c>/api/auth/promjena-lozinke</c>,
    /// gdje se stara lozinka trazi.
    /// </summary>
    [HttpPost("{id:int}/reset-lozinke")]
    public async Task<IActionResult> ResetujLozinkuAsync(
        int id, [FromBody] AdminResetLozinkeRequest request, CancellationToken ct)
    {
        await _korisnikService.ResetujLozinkuAsync(id, request, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/blokiraj")]
    public async Task<KorisnikDto> BlokirajAsync(int id, CancellationToken ct)
    {
        return await _korisnikService.BlokirajAsync(id, ct);
    }

    [HttpPost("{id:int}/odblokiraj")]
    public async Task<KorisnikDto> OdblokirajAsync(int id, CancellationToken ct)
    {
        return await _korisnikService.OdblokirajAsync(id, ct);
    }

    /// <summary>
    /// Brisanje naloga ne postoji - zahtjev se izvrsava kao deaktivacija.
    ///
    /// Rezervacije, placanja i recenzije korisnika moraju ostati, inace izvjestaji o
    /// prihodu i iskoristenosti flote govore neistinu. Deaktiviran nalog se ne moze
    /// prijaviti, a historija ostaje citava.
    /// </summary>
    [HttpDelete("{id:int}")]
    public override async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        return await base.DeleteAsync(id, ct);
    }
}
