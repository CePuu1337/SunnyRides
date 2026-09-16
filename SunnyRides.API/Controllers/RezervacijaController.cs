using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Rezervacije;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Rezervacije.
///
/// Citanje je otvoreno svakom prijavljenom korisniku, ali servis suzava rezultat:
/// klijent vidi iskljucivo svoje, osoblje sve. Ogranicenje je u servisu, a ne u
/// filteru koji bi klijent trebao poslati - na filter koji stize izvana se ne oslanja.
/// </summary>
[ApiController]
[Authorize]
[Route("api/rezervacije")]
public class RezervacijaController : ControllerBase
{
    private readonly IRezervacijaService _rezervacijaService;

    public RezervacijaController(IRezervacijaService rezervacijaService)
    {
        _rezervacijaService = rezervacijaService;
    }

    [HttpGet]
    public async Task<PagedResult<RezervacijaDto>> GetAsync(
        [FromQuery] RezervacijaSearchObject search, CancellationToken ct)
    {
        return await _rezervacijaService.GetAsync(search, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<RezervacijaDto> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _rezervacijaService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Kreiranje rezervacije. Iskljucivo za klijenta - rezervise za sebe, a ko je to
    /// cita se iz tokena.
    ///
    /// Rucni unos rezervacije od strane osoblja, iz kalendara flote, trazi da se
    /// klijent navede izvana. To je zaseban endpoint sa vlastitom provjerom uloge i
    /// dolazi uz kalendar; ovaj put se time ne otvara.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Uloge.Klijent)]
    public async Task<RezervacijaDto> KreirajAsync(
        [FromBody] RezervacijaInsertRequest request, CancellationToken ct)
    {
        return await _rezervacijaService.KreirajAsync(request, ct);
    }

    /// <summary>
    /// Koliko bi se vratilo kad bi se rezervacija otkazala sada. Nista ne mijenja.
    ///
    /// Nema ogranicenja na ulogu: klijentu treba za vlastitu rezervaciju, osoblju za
    /// tudju. Ko sta smije vidjeti odlucuje servis po korisniku iz tokena.
    /// </summary>
    [HttpGet("{id:int}/obracun-otkazivanja")]
    public async Task<ObracunOtkazivanjaDto> ObracunOtkazivanjaAsync(int id, CancellationToken ct)
    {
        return await _rezervacijaService.ObracunOtkazivanjaAsync(id, ct);
    }

    /// <summary>
    /// Otkazivanje.
    ///
    /// Otkazuju i klijent i osoblje, pa ovdje nema <c>Roles</c> - razlika se ne vidi
    /// u ruti nego u ishodu: kad otkazuje agencija, povrat je pun, a razlog je obavezan.
    /// Tijelo zahtjeva nosi samo razlog; iznos, status i izvrsilac su serverski.
    ///
    /// Tijelo smije izostati, pa klijent koji ne navodi razlog salje prazan POST.
    /// </summary>
    [HttpPost("{id:int}/otkazi")]
    public async Task<RezervacijaDto> OtkaziAsync(
        int id, [FromBody] OtkazivanjeRequest? request, CancellationToken ct)
    {
        return await _rezervacijaService.OtkaziAsync(id, request ?? new OtkazivanjeRequest(), ct);
    }
}
