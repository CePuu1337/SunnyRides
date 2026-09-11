using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Citanje: lista sa paginacijom i pretragom, te dohvat po identifikatoru.
///
/// Kontroler prima zahtjev, poziva servis i vraca DTO. Ne sadrzi poslovnu logiku
/// i ne pristupa DbContext-u. Rutu definise svaki konkretni kontroler.
/// </summary>
[ApiController]
public abstract class BaseController<TModel, TSearch> : ControllerBase
    where TSearch : BaseSearchObject
{
    protected readonly IService<TModel, TSearch> Service;

    protected BaseController(IService<TModel, TSearch> service)
    {
        Service = service;
    }

    [HttpGet]
    public virtual async Task<PagedResult<TModel>> GetAsync(
        [FromQuery] TSearch search, CancellationToken ct)
    {
        return await Service.GetAsync(search, ct);
    }

    [HttpGet("{id:int}")]
    public virtual async Task<TModel> GetByIdAsync(int id, CancellationToken ct)
    {
        return await Service.GetByIdAsync(id, ct);
    }
}
