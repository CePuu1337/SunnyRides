using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Oprema;

public interface IStanjeOpremeService
    : ICRUDService<StanjeOpremeDto, StanjeOpremeSearchObject, StanjeOpremeInsertRequest, StanjeOpremeUpdateRequest>
{
}
