using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Kalendar;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Kalendar flote - vremenski pregled zauzetosti, jedan red po vozilu.
///
/// Sluzi osoblju i zato je zatvoren za klijente: pokazuje ko je sve i kada uzeo koje
/// vozilo, a to su tudji podaci.
/// </summary>
[ApiController]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
[Route("api/kalendar-flote")]
public class KalendarController : ControllerBase
{
    private readonly IKalendarService _kalendarService;

    public KalendarController(IKalendarService kalendarService)
    {
        _kalendarService = kalendarService;
    }

    /// <summary>Bez zadatog perioda vraca tekucu sedmicu, od ponedjeljka.</summary>
    [HttpGet]
    public async Task<KalendarFloteDto> GetAsync(
        [FromQuery] KalendarSearchObject search, CancellationToken ct)
    {
        return await _kalendarService.KalendarAsync(search, ct);
    }
}
