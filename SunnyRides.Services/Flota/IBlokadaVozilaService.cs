using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Flota;

public interface IBlokadaVozilaService
    : ICRUDService<BlokadaVozilaDto, BlokadaVozilaSearchObject,
                   BlokadaVozilaInsertRequest, BlokadaVozilaUpdateRequest>
{
}
