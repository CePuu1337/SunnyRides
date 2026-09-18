using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Notifikacije;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Obavjestenja prijavljenog korisnika.
///
/// Nijedna ruta ne prima identifikator korisnika. Lista je vec suzena u servisu, a
/// pojedinacno obavjestenje se prije odgovora provjerava prema vlasniku iz tokena.
///
/// Obavjestenja se ne unose i ne brisu kroz API. Nastaju iskljucivo kao posljedica
/// dogadjaja u sistemu, pa endpointi za unos ne bi imali ko da ih zove osim nekoga ko
/// zeli tudjem korisniku poslati poruku.
/// </summary>
[Route("api/notifikacije")]
public class NotifikacijaController : BaseController<NotifikacijaDto, NotifikacijaSearchObject>
{
    private readonly INotifikacijaService _notifikacijaService;

    public NotifikacijaController(INotifikacijaService notifikacijaService)
        : base(notifikacijaService)
    {
        _notifikacijaService = notifikacijaService;
    }

    /// <summary>
    /// Broj neprocitanih, za oznaku na zvonu.
    ///
    /// Aplikacija ovo zove jednom, pri pokretanju. Dalje broj stize kroz SignalR uz
    /// svako novo obavjestenje, pa nema periodicnog pitanja serveru.
    /// </summary>
    [HttpGet("broj-neprocitanih")]
    public async Task<BrojNeprocitanihDto> BrojNeprocitanihAsync(CancellationToken ct)
    {
        return await _notifikacijaService.BrojNeprocitanihAsync(ct);
    }

    /// <summary>Oznacava jedno obavjestenje procitanim. Ponovljen poziv vraca isto stanje.</summary>
    [HttpPost("{id:int}/procitaj")]
    public async Task<NotifikacijaDto> OznaciProcitanuAsync(int id, CancellationToken ct)
    {
        return await _notifikacijaService.OznaciProcitanuAsync(id, ct);
    }

    /// <summary>Oznacava sva obavjestenja procitanim i vraca novi broj neprocitanih.</summary>
    [HttpPost("procitaj-sve")]
    public async Task<BrojNeprocitanihDto> OznaciSveProcitaneAsync(CancellationToken ct)
    {
        return await _notifikacijaService.OznaciSveProcitaneAsync(ct);
    }
}
