using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Flota;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Sezonske tarife.
///
/// Citanje je otvoreno svakom prijavljenom korisniku, jer ekran sa detaljima vozila
/// mora klijentu prikazati pragove popusta koji se stvarno primjenjuju pri obracunu.
/// Izmjena je iskljucivo administratorska - specifikacija kaze da cjenovnik ne vidi
/// ni uposlenik.
/// </summary>
[Route("api/cjenovnici")]
public class CjenovnikController
    : BaseCRUDController<CjenovnikDto, CjenovnikSearchObject,
                         CjenovnikInsertRequest, CjenovnikUpdateRequest>
{
    private readonly ICjenovnikService _cjenovnikService;

    public CjenovnikController(ICjenovnikService service) : base(service)
    {
        _cjenovnikService = service;
    }

    /// <summary>Tarifa koja za zadati model vazi na zadati dan.</summary>
    [HttpGet("vazeci")]
    public async Task<CjenovnikDto?> VazeciAsync(
        [FromQuery] int modelVozilaId, [FromQuery] DateTime? datum, CancellationToken ct)
    {
        return await _cjenovnikService.VazeciAsync(modelVozilaId, datum ?? DateTime.UtcNow, ct);
    }

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<CjenovnikDto> InsertAsync(
        [FromBody] CjenovnikInsertRequest request, CancellationToken ct)
        => base.InsertAsync(request, ct);

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<CjenovnikDto> UpdateAsync(
        int id, [FromBody] CjenovnikUpdateRequest request, CancellationToken ct)
        => base.UpdateAsync(id, request, ct);

    [Authorize(Roles = Uloge.Administrator)]
    public override Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
        => base.DeleteAsync(id, ct);
}
