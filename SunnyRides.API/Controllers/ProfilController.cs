using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Korisnici;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Vlastiti nalog.
///
/// Nijedna ruta nema identifikator korisnika - vlasnik se cita iz tokena, pa se tudji
/// profil ne moze ni adresirati. Zato ovdje nema ni provjere vlasnistva: nema sta da se
/// provjerava.
///
/// Forma za profil ne sadrzi lozinku. Promjena lozinke je zasebna radnja
/// (<c>/api/auth/promjena-lozinke</c>) i trazi staru lozinku.
/// </summary>
[ApiController]
[Authorize]
[Route("api/profil")]
public class ProfilController : ControllerBase
{
    private readonly IKorisnikService _korisnikService;

    public ProfilController(IKorisnikService korisnikService)
    {
        _korisnikService = korisnikService;
    }

    [HttpGet]
    public async Task<KorisnikDto> MojProfilAsync(CancellationToken ct)
    {
        return await _korisnikService.MojProfilAsync(ct);
    }

    [HttpPut]
    public async Task<KorisnikDto> AzurirajAsync(
        [FromBody] ProfilUpdateRequest request, CancellationToken ct)
    {
        return await _korisnikService.AzurirajProfilAsync(request, ct);
    }

    [HttpPost("slika")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<KorisnikDto> PostaviSlikuAsync(IFormFile fajl, CancellationToken ct)
    {
        if (fajl is null || fajl.Length == 0)
        {
            throw new BusinessException("Odaberite profilnu sliku.");
        }

        await using var sadrzaj = fajl.OpenReadStream();

        return await _korisnikService.PostaviSlikuAsync(sadrzaj, fajl.Length, ct);
    }

    [HttpDelete("slika")]
    public async Task<KorisnikDto> UkloniSlikuAsync(CancellationToken ct)
    {
        return await _korisnikService.UkloniSlikuAsync(ct);
    }
}
