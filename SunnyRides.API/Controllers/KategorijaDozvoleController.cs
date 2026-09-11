using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Kategorije vozackih dozvola: A1, A, B.</summary>
[Route("api/kategorije-dozvola")]
public class KategorijaDozvoleController
    : SifrarnikController<KategorijaDozvoleDto, KategorijaDozvoleSearchObject, KategorijaDozvoleInsertRequest, KategorijaDozvoleUpdateRequest>
{
    public KategorijaDozvoleController(IKategorijaDozvoleService service) : base(service)
    {
    }
}
