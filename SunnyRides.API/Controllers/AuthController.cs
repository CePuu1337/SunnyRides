using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Auth;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Jedini kontroler u projektu sa [AllowAnonymous] metodama. To su prijava,
/// registracija i dva koraka reseta zaboravljene lozinke - put koji po prirodi
/// stvari ne moze traziti token, jer korisnik ne moze ni doci do njega. Sve ostalo
/// u cijelom API-ju trazi vazeci token.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<PrijavaOdgovorDto> PrijaviAsync(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        return await _authService.PrijaviAsync(request, ct);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<KorisnikDto> RegistrujAsync(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        return await _authService.RegistrujAsync(request, ct);
    }

    /// <summary>
    /// Odgovor je 204 i kad nalog sa tom adresom ne postoji. Klijentu se prikazuje
    /// ista poruka u oba slucaja, pa se preko ovog endpointa ne moze saznati ko je
    /// registrovan.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("zaboravljena-lozinka")]
    public async Task<IActionResult> ZatraziResetAsync(
        [FromBody] ZaboravljenaLozinkaRequest request, CancellationToken ct)
    {
        await _authService.ZatraziResetAsync(request, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("reset-lozinke")]
    public async Task<IActionResult> PotvrdiResetAsync(
        [FromBody] ResetLozinkeRequest request, CancellationToken ct)
    {
        await _authService.PotvrdiResetAsync(request, ct);
        return NoContent();
    }

    [HttpGet("ja")]
    public async Task<KorisnikDto> TrenutniKorisnikAsync(CancellationToken ct)
    {
        return await _authService.TrenutniKorisnikAsync(ct);
    }

    [HttpPost("promjena-lozinke")]
    public async Task<IActionResult> PromijeniLozinkuAsync(
        [FromBody] PromjenaLozinkeRequest request, CancellationToken ct)
    {
        await _authService.PromijeniLozinkuAsync(request, ct);
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> OdjaviAsync(CancellationToken ct)
    {
        await _authService.OdjaviAsync(ct);
        return NoContent();
    }
}
