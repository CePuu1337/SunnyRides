using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Sifrarnik gradova.</summary>
[Route("api/gradovi")]
public class GradController
    : SifrarnikController<GradDto, GradSearchObject, GradInsertRequest, GradUpdateRequest>
{
    public GradController(IGradService service) : base(service)
    {
    }
}
