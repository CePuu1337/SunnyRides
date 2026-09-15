using SunnyRides.Model.DTOs;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Dostupnost;

/// <summary>
/// Jedino mjesto gdje se odlucuje je li vozilo slobodno.
///
/// Zove se sa tri strane i sve tri moraju dobiti isti odgovor: pretraga vozila,
/// kreiranje rezervacije i kalendar flote. Dvije implementacije istog uslova znace
/// da pretraga pokaze vozilo koje rezervacija odbije, ili gore - da rezervacija
/// prihvati termin koji je vec zauzet.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Dodaje uslov slobodnog termina na postojeci upit nad vozilima.
    ///
    /// Vraca <see cref="IQueryable{T}"/>, a ne listu identifikatora, da bi uslov
    /// ostao dio istog SQL upita. Alternativa - dohvatiti identifikatore slobodnih
    /// vozila pa ih proslijediti kroz Contains - znacila bi dva upita i listu koja
    /// raste sa velicinom flote.
    /// </summary>
    IQueryable<Vozilo> DodajUslovSlobodno(IQueryable<Vozilo> upit, DateTime datumOd, DateTime datumDo);

    Task<bool> JeSlobodnoAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default);

    /// <summary>Puna slika dostupnosti za jedno vozilo, sa razlogom kad nije slobodno.</summary>
    Task<DostupnostDto> ProvjeriAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default);

    /// <summary>Baca <c>BusinessException</c> ako vozilo nije slobodno. Koristi se prije upisa rezervacije.</summary>
    Task ObaveznoSlobodnoAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default);

    Task<List<int>> SlobodnaVozilaAsync(
        int? poslovnicaId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default);

    /// <summary>
    /// Potvrdjene i jos vazece rezervacije koje se preklapaju sa zadatim periodom.
    /// Uposlenik ih vidi prije nego unese blokadu, da odluci hoce li ponuditi
    /// zamjensko vozilo ili otkazati uz puni povrat.
    /// </summary>
    Task<List<PogodjenaRezervacijaDto>> PogodjeneRezervacijeAsync(
        int voziloId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default);

    /// <summary>
    /// Zakljucava red vozila do kraja tekuce transakcije, da dva istovremena zahtjeva
    /// za isti termin ne mogu oba proci provjeru.
    /// </summary>
    Task ZakljucajVoziloAsync(int voziloId, CancellationToken ct = default);
}
