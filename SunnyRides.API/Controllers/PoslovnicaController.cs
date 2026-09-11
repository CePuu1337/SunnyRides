using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Fizicke lokacije agencije.</summary>
[Route("api/poslovnice")]
public class PoslovnicaController
    : SifrarnikController<PoslovnicaDto, PoslovnicaSearchObject, PoslovnicaInsertRequest, PoslovnicaUpdateRequest>
{
    public PoslovnicaController(IPoslovnicaService service) : base(service)
    {
    }
}
