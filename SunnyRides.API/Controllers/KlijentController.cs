using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Korisnici;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Pretraga klijenata za rucni unos rezervacije iz kalendara flote.
///
/// Zaseban kontroler zato sto <see cref="KorisnikController"/> na klasi trazi
/// administratora, a ASP.NET Core atribute sa klase i metode sabira - uposlenik bi bio
/// odbijen bez obzira na to sta pise na metodi. Ovdje je uloga postavljena jednom, za
/// jednu rutu, i nema sta da je nadjaca.
///
/// Ruta ne otvara upravljanje nalozima: vraca samo aktivne klijente i samo ono sto treba
/// da ih uposlenik prepozna, bez naloga osoblja i bez uloga.
/// </summary>
[ApiController]
[Route("api/korisnici/klijenti")]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
public class KlijentController : ControllerBase
{
    private readonly IKorisnikService _korisnikService;

    public KlijentController(IKorisnikService korisnikService)
    {
        _korisnikService = korisnikService;
    }

    [HttpGet]
    public async Task<PagedResult<KlijentZaOdabirDto>> GetAsync(
        [FromQuery] KlijentSearchObject search, CancellationToken ct)
    {
        return await _korisnikService.KlijentiZaOdabirAsync(search, ct);
    }
}
