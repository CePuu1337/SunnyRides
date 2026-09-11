using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Benzin, dizel, elektricni.</summary>
[Route("api/tipovi-goriva")]
public class TipGorivaController
    : SifrarnikController<TipGorivaDto, TipGorivaSearchObject, TipGorivaInsertRequest, TipGorivaUpdateRequest>
{
    public TipGorivaController(ITipGorivaService service) : base(service)
    {
    }
}
