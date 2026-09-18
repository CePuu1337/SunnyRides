using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Rezervacije. Citanje je ograniceno vlasnistvom: klijent vidi iskljucivo svoje,
/// osoblje sve. To ogranicenje se nalaze u servisu, a ne se ocekuje od klijenta da
/// posalje ispravan filter.
/// </summary>
public interface IRezervacijaService : IService<RezervacijaDto, RezervacijaSearchObject>
{
    /// <summary>
    /// Kreira rezervaciju u jednoj transakciji: provjera dozvole, zakljucavanje
    /// vozila, provjera dostupnosti, obracun cijene na serveru, upis.
    /// </summary>
    Task<RezervacijaDto> KreirajAsync(RezervacijaInsertRequest request, CancellationToken ct = default);

    /// <summary>
    /// Rucni unos rezervacije od strane osoblja, za navedenog klijenta. Prolazi kroz
    /// iste provjere i zavrsava u istom statusu kao i klijentski unos.
    /// </summary>
    Task<RezervacijaDto> KreirajZaKlijentaAsync(
        int klijentId, RezervacijaInsertRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sta bi se vratilo kad bi se rezervacija otkazala u ovom trenutku. Nista ne
    /// mijenja - sluzi da klijent vidi posljedicu prije nego potvrdi.
    /// </summary>
    Task<ObracunOtkazivanjaDto> ObracunOtkazivanjaAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Otkazuje rezervaciju i evidentira povrat. Iznos se racuna iznova na serveru,
    /// ne uzima se iz prethodnog poziva obracuna.
    /// </summary>
    Task<RezervacijaDto> OtkaziAsync(
        int id, OtkazivanjeRequest request, CancellationToken ct = default);
}
