using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Sifrarnici;

public interface IDrzavaService
    : ICRUDService<DrzavaDto, DrzavaSearchObject, DrzavaInsertRequest, DrzavaUpdateRequest>
{
}
