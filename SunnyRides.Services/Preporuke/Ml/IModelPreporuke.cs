using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Preporuke.Ml;

/// <summary>
/// Model preporuke: matricna faktorizacija nad matricom korisnik x model vozila.
///
/// Za razliku od bodovanja sa unaprijed zadatim tezinama, ovdje se parametri **uce**
/// iz podataka - iz ocjena koje su korisnici stvarno dali. Model nauci latentne faktore
/// i po njima predvidja ocjenu koju bi korisnik dao vozilu koje jos nije vozio.
///
/// Servis je singleton: treniranje je skupo u odnosu na predikciju, pa se model drzi u
/// memoriji. Trenira ga pozadinski servis u API-ju, pri pokretanju i periodicno, tako da
/// zahtjev za preporukama nikad ne ceka na ucenje.
/// </summary>
public interface IModelPreporuke
{
    StanjeModelaDto Stanje { get; }

    /// <summary>
    /// Treniranje od pocetka nad trenutnim podacima. Dva istovremena poziva se ne
    /// preklapaju - drugi ceka da prvi zavrsi.
    /// </summary>
    Task<StanjeModelaDto> TrenirajAsync(CancellationToken ct = default);

    /// <summary>
    /// Je li korisnik bio u podacima za ucenje. Ako nije, model o njemu nema nista da
    /// kaze i preporuke idu rezervnim putem.
    /// </summary>
    bool ZnaKorisnika(int korisnikId);

    /// <summary>Predvidjena ocjena (1-5) po modelu vozila.</summary>
    IReadOnlyDictionary<int, double> PredvidiOcjene(
        int korisnikId, IReadOnlyCollection<int> modelVozilaIds);
}
