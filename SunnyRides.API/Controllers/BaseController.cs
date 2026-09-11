using Microsoft.AspNetCore.Authorization;
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
///
/// [Authorize] stoji ovdje, na bazi, a ne na svakom kontroleru ponaosob. Tako se
/// novi kontroler ne moze zaboraviti zastititi - zastita je podrazumijevano stanje,
/// a otvaranje endpointa trazi svjestan potez ([AllowAnonymous]), koji u projektu
/// postoji samo na prijavi i registraciji.
/// </summary>
[ApiController]
[Authorize]
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
