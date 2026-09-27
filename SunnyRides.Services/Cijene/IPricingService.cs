using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;

namespace SunnyRides.Services.Cijene;

/// <summary>
/// Jedino mjesto u sistemu gdje se racuna cijena najma.
///
/// Zove se sa dva mjesta i oba moraju dobiti isti broj: iz pregleda cijene koji
/// klijent vidi prije rezervacije, i iz kreiranja rezervacije. Da postoje dvije
/// implementacije, klijent bi vidio jednu cijenu a platio drugu.
/// </summary>
public interface IPricingService
{
    Task<CijenaRezervacijeDto> IzracunajAsync(
        int voziloId,
        DateTime datumOd,
        DateTime datumDo,
        IReadOnlyList<StavkaOpremeRequest> oprema,
        int? paketOsiguranjaId,
        CancellationToken ct = default);

    /// <summary>
    /// Cijena najma za isti termin za vise vozila odjednom - za rezultate pretrage.
    ///
    /// Isti obracun kao <see cref="IzracunajAsync"/>, bez opreme i osiguranja. Vozila
    /// se citaju jednim upitom, a sezonske tarife iz kesa, pa stranica od deset vozila
    /// ne pravi deset obracuna sa po nekoliko upita.
    /// </summary>
    Task<Dictionary<int, CijenaRezervacijeDto>> IzracunajZaVozilaAsync(
        IReadOnlyCollection<int> voziloIds,
        DateTime datumOd,
        DateTime datumDo,
        CancellationToken ct = default);

    /// <summary>
    /// Cijena jednog dana najma za dati datum, sa sezonskim mnoziocem. Koristi se za
    /// doplatu kad je vozilo vraceno kasnije od ugovorenog.
    /// </summary>
    Task<decimal> DnevnaCijenaAsync(int voziloId, DateTime datum, CancellationToken ct = default);
}
