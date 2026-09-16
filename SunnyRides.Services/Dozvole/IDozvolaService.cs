using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Dozvole;

/// <summary>
/// Vozacke dozvole i odgovor na pitanje sta klijent smije voziti.
///
/// <see cref="DozvoljeneKategorijeIdAsync"/> je jedina implementacija te logike i
/// poziva se sa tri mjesta: iz pretrage vozila, iz preporuka i iz provjere preduslova
/// pri rezervaciji. Da postoje dvije, klijent bi u pretrazi vidio vozilo koje mu
/// rezervacija odbija.
/// </summary>
public interface IDozvolaService : IService<VozackaDozvolaDto, VozackaDozvolaSearchObject>
{
    /// <summary>Dozvola prijavljenog korisnika, ili null ako je jos nije prijavio.</summary>
    Task<VozackaDozvolaDto?> MojaAsync(CancellationToken ct = default);

    /// <summary>
    /// Prijava ili izmjena vlastite dozvole. Svaka izmjena vraca status na
    /// <c>NaCekanju</c> - izmijenjenu dozvolu uposlenik mora ponovo pogledati.
    /// </summary>
    Task<VozackaDozvolaDto> PrijaviAsync(VozackaDozvolaRequest request, CancellationToken ct = default);

    Task<VozackaDozvolaDto> OdobriAsync(int id, CancellationToken ct = default);

    Task<VozackaDozvolaDto> OdbijAsync(int id, OdbijDozvoluRequest request, CancellationToken ct = default);

    /// <summary>Puna slika sa obrazlozenjem, za prikaz klijentu i uposleniku.</summary>
    Task<DozvoljeneKategorijeDto> DozvoljeneKategorijeAsync(
        int korisnikId, DateTime? naDan = null, CancellationToken ct = default);

    /// <summary>
    /// Sta bi vlasnik dozvole smio voziti kad bi dozvola bila odobrena.
    ///
    /// Racuna se iz kategorija upisanih na dozvoli i godina klijenta, **bez obzira na
    /// status verifikacije** - uposlenik to gleda dok odlucuje hoce li odobriti, pa
    /// mu odgovor "ceka verifikaciju" ne bi pomogao.
    /// </summary>
    Task<DozvoljeneKategorijeDto> PokrivenostZaDozvoluAsync(int dozvolaId, CancellationToken ct = default);

    /// <summary>Samo identifikatori - ono sto pretraga i provjera preduslova trebaju.</summary>
    Task<List<int>> DozvoljeneKategorijeIdAsync(
        int korisnikId, DateTime? naDan = null, CancellationToken ct = default);

    /// <summary>
    /// Baca <c>BusinessException</c> sa konkretnom porukom ako klijent ne smije voziti
    /// to vozilo na taj datum. Poziva se prije upisa rezervacije.
    /// </summary>
    Task ObaveznoSmijeVozitiAsync(
        int korisnikId, int voziloId, DateTime datumPreuzimanja, CancellationToken ct = default);
}
