using SunnyRides.Model;
using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Base;

/// <summary>Citanje sa paginacijom, filtriranjem i sortiranjem.</summary>
public interface IService<TModel, TSearch>
    where TSearch : BaseSearchObject
{
    Task<PagedResult<TModel>> GetAsync(TSearch search, CancellationToken ct = default);

    Task<TModel> GetByIdAsync(int id, CancellationToken ct = default);
}
