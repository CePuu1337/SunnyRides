using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Dostupnost;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Provjera zauzetosti vozila.
///
/// Isti servis koji odgovara ovdje odgovara i pretrazi i kreiranju rezervacije, pa
/// se ne moze desiti da klijentu vozilo izgleda slobodno a rezervacija ga odbije.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dostupnost")]
public class DostupnostController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public DostupnostController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>Je li jedno vozilo slobodno u zadatom terminu, i ako nije - zasto.</summary>
    [HttpGet("vozila/{voziloId:int}")]
    public async Task<DostupnostDto> ProvjeriAsync(
        int voziloId,
        [FromQuery] DateTime datumOd,
        [FromQuery] DateTime datumDo,
        [FromQuery] int? ignorisiRezervacijuId,
        CancellationToken ct)
    {
        return await _availabilityService.ProvjeriAsync(
            voziloId, datumOd, datumDo, ignorisiRezervacijuId, ct);
    }

    /// <summary>
    /// Rezervacije koje bi planirana blokada pogodila.
    ///
    /// Vraca kontakt podatke klijenata, pa je iskljucivo za osoblje. Klijent nema
    /// razloga znati ko jos ima rezervaciju na tom vozilu.
    /// </summary>
    [HttpGet("pogodjene-rezervacije")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<List<PogodjenaRezervacijaDto>> PogodjeneRezervacijeAsync(
        [FromQuery] int voziloId,
        [FromQuery] DateTime datumOd,
        [FromQuery] DateTime datumDo,
        CancellationToken ct)
    {
        return await _availabilityService.PogodjeneRezervacijeAsync(voziloId, datumOd, datumDo, ct);
    }
}
