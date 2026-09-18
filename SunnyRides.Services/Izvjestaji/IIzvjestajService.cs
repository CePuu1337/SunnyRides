using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Izvjestaji;

/// <summary>
/// Dva izvjestaja koja uputstvo trazi: iskoristenost flote i finansijski pregled.
///
/// Svaki postoji u dva oblika - kao podatak i kao PDF. Podatak sluzi za pregled prije
/// generisanja, da korisnik provjeri parametre, a PDF se gradi **iz tog istog podatka**,
/// pa se ne moze desiti da pregled pokaze jedno a dokument drugo.
///
/// PDF se generise na serveru. Da se generise u Flutteru, logika izvjestaja bi postojala
/// na dva mjesta i agregacije bi se racunale u aplikaciji umjesto na bazi.
/// </summary>
public interface IIzvjestajService
{
    Task<IskoristenostFloteDto> IskoristenostFloteAsync(
        IzvjestajSearchObject search, CancellationToken ct = default);

    Task<FinansijskiPregledDto> FinansijskiPregledAsync(
        IzvjestajSearchObject search, CancellationToken ct = default);

    Task<byte[]> IskoristenostFlotePdfAsync(
        IzvjestajSearchObject search, CancellationToken ct = default);

    Task<byte[]> FinansijskiPregledPdfAsync(
        IzvjestajSearchObject search, CancellationToken ct = default);
}
