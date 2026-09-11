using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Base;

public interface ICRUDService<TModel, TSearch, TInsert, TUpdate> : IService<TModel, TSearch>
    where TSearch : BaseSearchObject
{
    Task<TModel> InsertAsync(TInsert request, CancellationToken ct = default);

    Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
