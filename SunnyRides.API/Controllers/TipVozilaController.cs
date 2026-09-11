using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Skuter, motocikl, quad.</summary>
[Route("api/tipovi-vozila")]
public class TipVozilaController
    : SifrarnikController<TipVozilaDto, TipVozilaSearchObject, TipVozilaInsertRequest, TipVozilaUpdateRequest>
{
    public TipVozilaController(ITipVozilaService service) : base(service)
    {
    }
}
