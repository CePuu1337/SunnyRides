using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Oprema;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Zalihe opreme po poslovnici.
///
/// Za osoblje: administrator vodi sve poslovnice, uposlenik samo svoju (to provjerava
/// servis). Klijent zalihe ne vidi kao listu - njemu se uz rezervaciju nudi samo
/// oprema koja je u njegovom terminu slobodna.
/// </summary>
[Route("api/stanja-opreme")]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
public class StanjeOpremeController
    : BaseCRUDController<StanjeOpremeDto, StanjeOpremeSearchObject,
                         StanjeOpremeInsertRequest, StanjeOpremeUpdateRequest>
{
    public StanjeOpremeController(IStanjeOpremeService service) : base(service)
    {
    }
}
