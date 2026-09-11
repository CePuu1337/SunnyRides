using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Katalog dodatne opreme.</summary>
[Route("api/vrste-opreme")]
public class VrstaOpremeController
    : SifrarnikController<VrstaOpremeDto, VrstaOpremeSearchObject, VrstaOpremeInsertRequest, VrstaOpremeUpdateRequest>
{
    public VrstaOpremeController(IVrstaOpremeService service) : base(service)
    {
    }
}
