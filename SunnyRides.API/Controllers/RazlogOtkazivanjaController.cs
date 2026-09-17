using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Razlozi otkazivanja za padajuce liste u obje aplikacije.</summary>
[Route("api/razlozi-otkazivanja")]
public class RazlogOtkazivanjaController
    : SifrarnikController<RazlogOtkazivanjaDto, RazlogOtkazivanjaSearchObject, RazlogOtkazivanjaInsertRequest, RazlogOtkazivanjaUpdateRequest>
{
    public RazlogOtkazivanjaController(IRazlogOtkazivanjaService service) : base(service)
    {
    }
}
