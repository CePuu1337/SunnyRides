using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Proizvodjaci vozila.</summary>
[Route("api/marke")]
public class MarkaController
    : SifrarnikController<MarkaDto, MarkaSearchObject, MarkaInsertRequest, MarkaUpdateRequest>
{
    public MarkaController(IMarkaService service) : base(service)
    {
    }
}
