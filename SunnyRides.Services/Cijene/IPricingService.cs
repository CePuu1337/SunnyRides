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
    /// Cijena jednog dana najma za dati datum, sa sezonskim mnoziocem. Koristi se za
    /// doplatu kad je vozilo vraceno kasnije od ugovorenog.
    /// </summary>
    Task<decimal> DnevnaCijenaAsync(int voziloId, DateTime datum, CancellationToken ct = default);
}
