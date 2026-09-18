using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Preporuke;

/// <summary>
/// Rangiranje vozila za konkretnog korisnika.
///
/// Rezultat prolazi kroz iste filtere kao i pretraga - dostupnost u terminu i
/// kategorije sa korisnikove dozvole. Preporuka vozila koje korisnik ne smije voziti
/// ili koje je zauzeto bila bi gora od nikakve preporuke.
/// </summary>
public interface IRecommenderService
{
    Task<PagedResult<PreporukaDto>> PreporuciAsync(
        PreporukaSearchObject search, CancellationToken ct = default);

    /// <summary>
    /// Vozila slicna zadatom, za ekran sa detaljima. Koristi isti racun slicnosti, samo
    /// sto profil ne dolazi iz korisnikove historije nego iz samog vozila koje gleda.
    /// </summary>
    Task<List<PreporukaDto>> SlicnaVozilaAsync(
        int voziloId, int? broj = null, CancellationToken ct = default);
}
