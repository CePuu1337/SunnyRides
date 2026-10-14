using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Sifrarnici;

public interface IPoslovnicaService
    : ICRUDService<PoslovnicaDto, PoslovnicaSearchObject, PoslovnicaInsertRequest, PoslovnicaUpdateRequest>
{
    /// <summary>
    /// Poslovnica najbliza prijavljenom klijentu - od lokacije telefona ako je
    /// poslana, inace od grada iz profila. Prazno kad se ne zna odakle mjeriti.
    /// </summary>
    Task<NajblizaPoslovnicaDto?> NajblizaAsync(
        double? latituda, double? longituda, CancellationToken ct = default);
}
