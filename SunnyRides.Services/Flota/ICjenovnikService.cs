using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Flota;

public interface ICjenovnikService
    : ICRUDService<CjenovnikDto, CjenovnikSearchObject, CjenovnikInsertRequest, CjenovnikUpdateRequest>
{
    /// <summary>
    /// Tarifa koja za zadati model vazi na zadati dan, ili null ako za taj dan nema
    /// definisane sezone. Ovo je jedina tacka kroz koju PricingService cita cjenovnik.
    /// </summary>
    Task<CjenovnikDto?> VazeciAsync(int modelVozilaId, DateTime datum, CancellationToken ct = default);
}
