using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Sifrarnik drzava.</summary>
[Route("api/drzave")]
public class DrzavaController
    : SifrarnikController<DrzavaDto, DrzavaSearchObject, DrzavaInsertRequest, DrzavaUpdateRequest>
{
    public DrzavaController(IDrzavaService service) : base(service)
    {
    }
}
