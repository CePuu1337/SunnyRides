using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Pregled;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Pocetni ekran administrativne aplikacije.
///
/// Jedan poziv vraca sve sto ekran prikazuje. Alternativa - sest poziva pa racunanje u
/// Flutteru - znacila bi da aplikacija povlaci sve rezervacije i sva placanja da bi
/// ispisala cetiri broja.
/// </summary>
[ApiController]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
[Route("api/pregled-poslovanja")]
public class PregledPoslovanjaController : ControllerBase
{
    private readonly IPregledService _pregledService;

    public PregledPoslovanjaController(IPregledService pregledService)
    {
        _pregledService = pregledService;
    }

    [HttpGet]
    public async Task<PregledPoslovanjaDto> GetAsync(CancellationToken ct)
    {
        return await _pregledService.PregledAsync(ct);
    }
}
