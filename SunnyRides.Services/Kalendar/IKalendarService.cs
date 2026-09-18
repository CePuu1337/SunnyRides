using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Kalendar;

/// <summary>
/// Kalendar flote: ko je gdje i kada zauzet.
///
/// Modul postoji zbog osnovnog problema ovog domena - dvostruko ugovorenog termina - pa
/// mu je najvaznije svojstvo da govori istinu. Zato blokove gradi iz istih zapisa koje
/// provjera dostupnosti smatra zauzecem, a ne iz vlastitog uslova.
/// </summary>
public interface IKalendarService
{
    Task<KalendarFloteDto> KalendarAsync(KalendarSearchObject search, CancellationToken ct = default);
}
