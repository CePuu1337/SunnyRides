using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Korisnici;

/// <summary>
/// Nalozi i uloge.
///
/// Administracija naloga i vlastiti profil su namjerno u istom servisu, ali su im rute i
/// pravila razdvojeni: administrator radi po identifikatoru nad tudjim nalozima, a
/// korisnik nad svojim - i tada identifikator ne salje nego se cita iz tokena.
/// </summary>
public interface IKorisnikService
    : ICRUDService<KorisnikDto, KorisnikSearchObject, KorisnikInsertRequest, KorisnikUpdateRequest>
{
    /// <summary>Uloge za padajucu listu. Uloge su fiksne i ne unose se kroz aplikaciju.</summary>
    Task<List<RoleDto>> UlogeAsync(CancellationToken ct = default);

    Task<KorisnikDto> PostaviUlogeAsync(int id, UlogeKorisnikaRequest request, CancellationToken ct = default);

    /// <summary>Postavlja novu lozinku bez trazenja stare. Smije je pozvati samo administrator.</summary>
    Task ResetujLozinkuAsync(int id, AdminResetLozinkeRequest request, CancellationToken ct = default);

    Task<KorisnikDto> BlokirajAsync(int id, CancellationToken ct = default);

    Task<KorisnikDto> OdblokirajAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Aktivni klijenti za odabir pri rucnom unosu rezervacije. Dostupno i uposleniku,
    /// pa vraca samo klijente i samo polja potrebna za odabir.
    /// </summary>
    Task<PagedResult<KlijentZaOdabirDto>> KlijentiZaOdabirAsync(
        KlijentSearchObject search, CancellationToken ct = default);

    // --- vlastiti profil ---------------------------------------------------

    Task<KorisnikDto> MojProfilAsync(CancellationToken ct = default);

    Task<KorisnikDto> AzurirajProfilAsync(ProfilUpdateRequest request, CancellationToken ct = default);

    Task<KorisnikDto> PostaviSlikuAsync(Stream sadrzaj, long duzinaBajta, CancellationToken ct = default);

    Task<KorisnikDto> UkloniSlikuAsync(CancellationToken ct = default);
}
