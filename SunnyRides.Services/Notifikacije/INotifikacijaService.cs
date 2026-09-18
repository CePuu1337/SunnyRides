using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Notifikacije;

/// <summary>
/// Obavjestenja korisniku - citanje vlastitih i upis novih.
///
/// Servis je jedino mjesto na kojem obavjestenje nastaje. Prije ovoga ih je upisivao
/// worker direktno kroz DbContext, pa bi svako novo mjesto koje salje obavjestenje
/// moralo ponoviti i upis i guranje na uredjaj - i prvo koje zaboravi ovo drugo dalo
/// bi obavjestenje koje se pojavi tek pri sljedecem otvaranju aplikacije.
/// </summary>
public interface INotifikacijaService : IService<NotifikacijaDto, NotifikacijaSearchObject>
{
    Task<BrojNeprocitanihDto> BrojNeprocitanihAsync(CancellationToken ct = default);

    Task<NotifikacijaDto> OznaciProcitanuAsync(int id, CancellationToken ct = default);

    /// <summary>Oznacava sva neprocitana kao procitana i vraca novi broj, koji je uvijek nula.</summary>
    Task<BrojNeprocitanihDto> OznaciSveProcitaneAsync(CancellationToken ct = default);

    /// <summary>
    /// Upisuje obavjestenje i gura ga na uredjaj korisnika.
    ///
    /// Zove se iz workera, gdje prijavljenog korisnika nema, pa primalac stize kao
    /// parametar. To je jedini slucaj u sistemu u kojem se identifikator korisnika ne
    /// cita iz tokena - a i tu ga ne bira klijent nego posao koji je dogadjaj izazvao.
    /// </summary>
    Task<NotifikacijaDto> KreirajAsync(
        int korisnikId, int? rezervacijaId, TipNotifikacije tip,
        string naslov, string tekst, CancellationToken ct = default);
}
