using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Recenzije;

/// <summary>
/// Recenzije nakon zavrsenog najma.
///
/// Klijent pise i mijenja iskljucivo svoju, i to tek kad je najam zavrsen. Osoblje ne
/// pise recenzije nego ih moderira - i to skrivanjem, nikad brisanjem, da prosjecna
/// ocjena ostane sljediva.
/// </summary>
public interface IRecenzijaService
    : ICRUDService<RecenzijaDto, RecenzijaSearchObject, RecenzijaInsertRequest, RecenzijaUpdateRequest>
{
    /// <summary>Sklanja recenziju iz javnog prikaza, prosjecne ocjene i preporuka.</summary>
    Task<RecenzijaDto> SakrijAsync(int id, CancellationToken ct = default);

    Task<RecenzijaDto> PrikaziAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Rezervacije prijavljenog korisnika koje cekaju recenziju - zavrsene, a jos
    /// neocijenjene. Mobilna aplikacija po ovome zna kada ponuditi ocjenjivanje.
    /// </summary>
    Task<List<RezervacijaZaRecenzijuDto>> ZaOcjenjivanjeAsync(CancellationToken ct = default);
}
