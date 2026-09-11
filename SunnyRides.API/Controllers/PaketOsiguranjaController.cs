using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Paketi osiguranja uz najam.</summary>
[Route("api/paketi-osiguranja")]
public class PaketOsiguranjaController
    : SifrarnikController<PaketOsiguranjaDto, PaketOsiguranjaSearchObject, PaketOsiguranjaInsertRequest, PaketOsiguranjaUpdateRequest>
{
    public PaketOsiguranjaController(IPaketOsiguranjaService service) : base(service)
    {
    }
}
