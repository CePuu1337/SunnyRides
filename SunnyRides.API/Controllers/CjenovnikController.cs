using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Flota;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Sezonske tarife. Cijeli modul je administratorski - specifikacija kaze da cjenovnik
/// ne vidi ni uposlenik.
///
/// Klijent tarife ne cita odavde. Cijenu koju placa na dati dan, sa sezonom i
/// popustima, dobija gotovu kroz <c>/api/cijene/vozilo/{id}</c> i uz vozila u pretrazi,
/// pa mu sirovi zapisi cjenovnika ne trebaju.
/// </summary>
[Route("api/cjenovnici")]
[Authorize(Roles = Uloge.Administrator)]
public class CjenovnikController
    : BaseCRUDController<CjenovnikDto, CjenovnikSearchObject,
                         CjenovnikInsertRequest, CjenovnikUpdateRequest>
{
    public CjenovnikController(ICjenovnikService service) : base(service)
    {
    }
}
