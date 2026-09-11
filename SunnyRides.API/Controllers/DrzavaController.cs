using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Sifrarnik drzava. Odrzavanje sifrarnika je administratorski posao, pa cijeli
/// kontroler trazi ulogu Administrator.
///
/// Kad klijentska aplikacija bude trebala listu drzava za formu (npr. pri unosu
/// vozacke dozvole), ta lista ce se citati kroz endpoint tog modula, a ne kroz
/// ovaj - sifrarnik se tamo samo cita, ovdje se i mijenja.
/// </summary>
[Route("api/drzave")]
[Authorize(Roles = Uloge.Administrator)]
public class DrzavaController
    : BaseCRUDController<DrzavaDto, DrzavaSearchObject, DrzavaInsertRequest, DrzavaUpdateRequest>
{
    public DrzavaController(IDrzavaService service) : base(service)
    {
    }
}
