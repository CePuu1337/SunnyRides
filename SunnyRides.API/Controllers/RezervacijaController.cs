using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Placanja;
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
    private readonly IPlacanjeService _placanjeService;

    public RezervacijaController(IRezervacijaService rezervacijaService, IPlacanjeService placanjeService)
    {
        _rezervacijaService = rezervacijaService;
        _placanjeService = placanjeService;
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
    /// Rucni unos rezervacije od strane osoblja trazi da se klijent navede izvana, pa
    /// ide kroz zaseban endpoint (<c>POST klijent/{klijentId}</c>) sa vlastitom
    /// provjerom uloge. Ovaj put se time ne otvara.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Uloge.Klijent)]
    public async Task<RezervacijaDto> KreirajAsync(
        [FromBody] RezervacijaInsertRequest request, CancellationToken ct)
    {
        return await _rezervacijaService.KreirajAsync(request, ct);
    }

    /// <summary>
    /// Rucni unos rezervacije od strane osoblja, iz kalendara flote.
    ///
    /// Klijent je ovdje u ruti, a ne u tijelu zahtjeva, i to je namjerno vidljivo: nije
    /// rijec o tome ko poziva - to se i dalje cita iz tokena i mora biti osoblje - nego
    /// o tome za koga se rezervise. Rezervacija prolazi kroz iste provjere i zavrsava u
    /// istom statusu kao da ju je klijent sam napravio.
    /// </summary>
    [HttpPost("klijent/{klijentId:int}")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<RezervacijaDto> KreirajZaKlijentaAsync(
        int klijentId, [FromBody] RezervacijaInsertRequest request, CancellationToken ct)
    {
        return await _rezervacijaService.KreirajZaKlijentaAsync(klijentId, request, ct);
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
    /// Otkazuju i klijent i osoblje, pa ovdje nema <c>Roles</c>. Razlika se vidi u
    /// ishodu, ne u ruti: kad otkazuje agencija, povrat je pun. Tijelo nosi samo
    /// odabrani razlog i napomenu, a iznos, status i izvrsioca odredjuje server.
    /// </summary>
    [HttpPost("{id:int}/otkazi")]
    public async Task<RezervacijaDto> OtkaziAsync(
        int id, [FromBody] OtkazivanjeRequest request, CancellationToken ct)
    {
        return await _rezervacijaService.OtkaziAsync(id, request, ct);
    }

    /// <summary>
    /// Priprema naplatu. Tijelo ne postoji - iznos, valutu i vezu sa rezervacijom
    /// odredjuje server. Klijent dobija samo ono sto PaymentSheet treba.
    /// </summary>
    [HttpPost("{id:int}/payment-intent")]
    [Authorize(Roles = Uloge.Klijent)]
    public async Task<PlatniIntentDto> KreirajIntentAsync(int id, CancellationToken ct)
    {
        return await _placanjeService.KreirajIntentAsync(id, ct);
    }
}
