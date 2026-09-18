using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Recenzije;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Recenzije nakon zavrsenog najma.
///
/// Klijent pise i mijenja iskljucivo svoju recenziju; osoblje ih moderira. Nijedna ruta
/// ne prima autora - on se cita iz tokena, a vozilo se izvodi iz rezervacije.
/// </summary>
[Route("api/recenzije")]
public class RecenzijaController
    : BaseCRUDController<RecenzijaDto, RecenzijaSearchObject, RecenzijaInsertRequest, RecenzijaUpdateRequest>
{
    private readonly IRecenzijaService _recenzijaService;

    public RecenzijaController(IRecenzijaService recenzijaService)
        : base(recenzijaService)
    {
        _recenzijaService = recenzijaService;
    }

    /// <summary>
    /// Zavrseni najmovi prijavljenog korisnika koji jos nisu ocijenjeni. Mobilna
    /// aplikacija po ovome zna kada uopste ponuditi ocjenjivanje.
    /// </summary>
    [HttpGet("za-ocjenjivanje")]
    public async Task<List<RezervacijaZaRecenzijuDto>> ZaOcjenjivanjeAsync(CancellationToken ct)
    {
        return await _recenzijaService.ZaOcjenjivanjeAsync(ct);
    }

    [HttpPost("{id:int}/sakrij")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<RecenzijaDto> SakrijAsync(int id, CancellationToken ct)
    {
        return await _recenzijaService.SakrijAsync(id, ct);
    }

    [HttpPost("{id:int}/prikazi")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<RecenzijaDto> PrikaziAsync(int id, CancellationToken ct)
    {
        return await _recenzijaService.PrikaziAsync(id, ct);
    }

    /// <summary>
    /// Brisanje recenzije ne postoji - zahtjev se izvrsava kao skrivanje.
    ///
    /// Uputstvo trazi da recenzija ostane u bazi kako bi prosjecna ocjena bila sljediva,
    /// a klijentska aplikacija ima dugme "obrisi" kao i svugdje. Umjesto da se zahtjev
    /// odbije porukom, radi ono sto je jedino ispravno i o tome nema iznenadjenja: zapis
    /// ostaje, ali se vise ne prikazuje niti ulazi u ocjenu.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public override async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        await _recenzijaService.SakrijAsync(id, ct);

        return NoContent();
    }
}
