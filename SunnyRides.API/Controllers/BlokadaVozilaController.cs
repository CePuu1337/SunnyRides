using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Flota;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Blokade vozila - servis, kvar, sezonsko povlacenje iz flote.
///
/// Cijeli kontroler je za osoblje. Klijent blokade ne vidi ni kao listu: za njega je
/// blokirano vozilo jednostavno vozilo koje se u pretrazi ne pojavljuje, a razlog
/// blokade je interni podatak agencije.
/// </summary>
[Route("api/blokade")]
[Authorize(Roles = Uloge.AdministratorIliUposlenik)]
public class BlokadaVozilaController
    : BaseCRUDController<BlokadaVozilaDto, BlokadaVozilaSearchObject,
                         BlokadaVozilaInsertRequest, BlokadaVozilaUpdateRequest>
{
    public BlokadaVozilaController(IBlokadaVozilaService service) : base(service)
    {
    }
}
