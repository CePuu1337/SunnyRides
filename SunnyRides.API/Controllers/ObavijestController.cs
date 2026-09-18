using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Obavijesti;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Objave agencije.
///
/// Citanje je otvoreno svakom prijavljenom korisniku, ali servis klijentu vraca samo
/// objavljene obavijesti. Pisanje je iskljucivo administratorsko.
/// </summary>
[Route("api/obavijesti")]
public class ObavijestController
    : BaseCRUDController<ObavijestDto, ObavijestSearchObject, ObavijestInsertRequest, ObavijestUpdateRequest>
{
    private readonly IObavijestService _obavijestService;

    public ObavijestController(IObavijestService obavijestService)
        : base(obavijestService)
    {
        _obavijestService = obavijestService;
    }

    [HttpPost]
    [Authorize(Roles = Uloge.Administrator)]
    public override async Task<ObavijestDto> InsertAsync(
        [FromBody] ObavijestInsertRequest request, CancellationToken ct)
    {
        return await base.InsertAsync(request, ct);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Uloge.Administrator)]
    public override async Task<ObavijestDto> UpdateAsync(
        int id, [FromBody] ObavijestUpdateRequest request, CancellationToken ct)
    {
        return await base.UpdateAsync(id, request, ct);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Uloge.Administrator)]
    public override async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        return await base.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Postavlja ili mijenja sliku obavijesti. Slika se snima na disk, u bazu ide samo
    /// putanja, a lista vraca URL - nikad sadrzaj.
    /// </summary>
    [HttpPost("{id:int}/slika")]
    [Authorize(Roles = Uloge.Administrator)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ObavijestDto> PostaviSlikuAsync(int id, IFormFile fajl, CancellationToken ct)
    {
        if (fajl is null || fajl.Length == 0)
        {
            throw new BusinessException("Odaberite sliku obavijesti.");
        }

        await using var sadrzaj = fajl.OpenReadStream();

        return await _obavijestService.PostaviSlikuAsync(id, sadrzaj, fajl.Length, ct);
    }

    [HttpDelete("{id:int}/slika")]
    [Authorize(Roles = Uloge.Administrator)]
    public async Task<ObavijestDto> UkloniSlikuAsync(int id, CancellationToken ct)
    {
        return await _obavijestService.UkloniSlikuAsync(id, ct);
    }
}
