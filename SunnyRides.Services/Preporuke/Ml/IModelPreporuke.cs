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
/// memoriji i osvjezava periodicno.
/// </summary>
public interface IModelPreporuke
{
    StanjeModelaDto Stanje { get; }

    /// <summary>Trenira ako model jos ne postoji ili je zastario. Inace ne radi nista.</summary>
    Task<StanjeModelaDto> OsvjeziAkoTrebaAsync(CancellationToken ct = default);

    /// <summary>Treniranje na zahtjev, bez obzira na starost modela.</summary>
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
