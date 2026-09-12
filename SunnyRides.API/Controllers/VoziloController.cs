using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Flota;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Flota. Citanje je otvoreno svakom prijavljenom korisniku, jer klijent u mobilnoj
/// aplikaciji pretrazuje vozila. Upravljanje flotom je posao osoblja.
/// </summary>
[Route("api/vozila")]
public class VoziloController
    : BaseCRUDController<VoziloDto, VoziloSearchObject, VoziloInsertRequest, VoziloUpdateRequest>
{
    public VoziloController(IVoziloService service) : base(service)
    {
    }

    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public override Task<VoziloDto> InsertAsync([FromBody] VoziloInsertRequest request, CancellationToken ct)
        => base.InsertAsync(request, ct);

    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public override Task<VoziloDto> UpdateAsync(
        int id, [FromBody] VoziloUpdateRequest request, CancellationToken ct)
        => base.UpdateAsync(id, request, ct);

    // Brisanje je administratorsko. Uposlenik vozilo moze deaktivirati kroz izmjenu,
    // sto je ionako ispravan potez za vozilo koje je ikad bilo izdato.
    [Authorize(Roles = Uloge.Administrator)]
    public override Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
        => base.DeleteAsync(id, ct);
}
