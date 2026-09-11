using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Auth;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Jedini kontroler u projektu koji ima [AllowAnonymous] metode, i to samo dvije -
/// prijavu i registraciju. Sve ostalo trazi vazeci token.
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
