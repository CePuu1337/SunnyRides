using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Controllers;

/// <summary>Fizicke lokacije agencije.</summary>
[Route("api/poslovnice")]
public class PoslovnicaController
    : SifrarnikController<PoslovnicaDto, PoslovnicaSearchObject, PoslovnicaInsertRequest, PoslovnicaUpdateRequest>
{
    private readonly IPoslovnicaService _poslovnice;

    public PoslovnicaController(IPoslovnicaService service) : base(service)
    {
        _poslovnice = service;
    }

    /// <summary>
    /// Poslovnica najbliza prijavljenom klijentu. Lokacija telefona je opciona i
    /// sluzi samo ovom upitu; bez nje se mjeri od grada iz profila. Kad se ne zna
    /// odakle mjeriti, odgovor je prazan (204).
    /// </summary>
    [HttpGet("najbliza")]
    public Task<NajblizaPoslovnicaDto?> NajblizaAsync(
        [FromQuery] double? latituda, [FromQuery] double? longituda, CancellationToken ct)
        => _poslovnice.NajblizaAsync(latituda, longituda, ct);
}
