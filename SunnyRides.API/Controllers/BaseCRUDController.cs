using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Nasljedjuje citanje i dodaje upis, izmjenu i brisanje. Zahvaljujuci ovoj klasi
/// kontroler za sifrarnik svodi se na nekoliko linija.
/// </summary>
public abstract class BaseCRUDController<TModel, TSearch, TInsert, TUpdate>
    : BaseController<TModel, TSearch>
    where TSearch : BaseSearchObject
{
    protected readonly ICRUDService<TModel, TSearch, TInsert, TUpdate> CRUDService;

    protected BaseCRUDController(ICRUDService<TModel, TSearch, TInsert, TUpdate> service)
        : base(service)
    {
        CRUDService = service;
    }

    [HttpPost]
    public virtual async Task<TModel> InsertAsync([FromBody] TInsert request, CancellationToken ct)
    {
        return await CRUDService.InsertAsync(request, ct);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<TModel> UpdateAsync(
        int id, [FromBody] TUpdate request, CancellationToken ct)
    {
        return await CRUDService.UpdateAsync(id, request, ct);
    }

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        await CRUDService.DeleteAsync(id, ct);
        return NoContent();
    }
}
