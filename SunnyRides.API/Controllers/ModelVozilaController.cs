using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Konkretni modeli, sa potrebnom kategorijom dozvole.</summary>
[Route("api/modeli-vozila")]
public class ModelVozilaController
    : SifrarnikController<ModelVozilaDto, ModelVozilaSearchObject, ModelVozilaInsertRequest, ModelVozilaUpdateRequest>
{
    public ModelVozilaController(IModelVozilaService service) : base(service)
    {
    }
}
