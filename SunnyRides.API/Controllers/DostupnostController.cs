using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Oprema;

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
    private readonly IDostupnostOpremeService _dostupnostOpreme;

    public DostupnostController(
        IAvailabilityService availabilityService, IDostupnostOpremeService dostupnostOpreme)
    {
        _availabilityService = availabilityService;
        _dostupnostOpreme = dostupnostOpreme;
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
    /// Oprema koja se uz vozilo moze uzeti u zadatom terminu, sa brojem slobodnih
    /// komada. Lista je kratka po prirodi - koliko ima vrsta opreme u katalogu.
    /// </summary>
    [HttpGet("vozila/{voziloId:int}/oprema")]
    public async Task<List<DostupnaOpremaDto>> OpremaAsync(
        int voziloId,
        [FromQuery] DateTime datumOd,
        [FromQuery] DateTime datumDo,
        CancellationToken ct)
    {
        return await _dostupnostOpreme.DostupnaZaVoziloAsync(voziloId, datumOd, datumDo, ct);
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
