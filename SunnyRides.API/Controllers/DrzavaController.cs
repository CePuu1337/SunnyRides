using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

[Route("api/drzave")]
public class DrzavaController
    : BaseCRUDController<DrzavaDto, DrzavaSearchObject, DrzavaInsertRequest, DrzavaUpdateRequest>
{
    public DrzavaController(IDrzavaService service) : base(service)
    {
    }
}
