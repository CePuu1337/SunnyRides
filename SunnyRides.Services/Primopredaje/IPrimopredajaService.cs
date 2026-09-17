using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Primopredaje;

public interface IPrimopredajaService : IService<PrimopredajaDto, PrimopredajaSearchObject>
{
    /// <summary>Izdavanje vozila. Status rezervacije ostaje Confirmed.</summary>
    Task<PrimopredajaDto> IzdajAsync(
        IzdavanjeVozilaRequest request, IReadOnlyList<UlazniFajl> fotografije, CancellationToken ct = default);

    /// <summary>Povrat vozila: obracun depozita, prelaz u Completed i povrat ostatka depozita.</summary>
    Task<PrimopredajaDto> VratiAsync(
        PovratVozilaRequest request, IReadOnlyList<UlazniFajl> fotografije, CancellationToken ct = default);

    /// <summary>Obracun koji bi vazio da se vozilo vrati u zadato vrijeme. Nista ne mijenja.</summary>
    Task<ObracunPovrataDto> ObracunPovrataAsync(
        int rezervacijaId, DateTime? datumPovrata, decimal? iznosStete, CancellationToken ct = default);

    Task<PagedResult<RasporedStavkaDto>> RasporedAsync(RasporedSearchObject search, CancellationToken ct = default);

    Task<PrivatniFajl> PreuzmiFotografijuAsync(int fotografijaId, CancellationToken ct = default);
}
