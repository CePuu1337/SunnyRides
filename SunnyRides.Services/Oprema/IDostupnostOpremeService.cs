using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;

namespace SunnyRides.Services.Oprema;

/// <summary>
/// Jedino mjesto koje odlucuje koliko je opreme slobodno u nekom terminu.
///
/// Koriste ga kreiranje rezervacije, zamjena vozila, izmjena zaliha i mobilna
/// aplikacija kad nudi opremu, pa svi gledaju isto pravilo.
/// </summary>
public interface IDostupnostOpremeService
{
    /// <summary>
    /// Baca BusinessException kad trazena oprema u tom terminu nije slobodna.
    /// Zakljucava zalihe poslovnice, pa mora biti pozvana unutar otvorene transakcije.
    /// </summary>
    Task ObaveznoDostupnoAsync(
        IReadOnlyCollection<StavkaOpremeRequest> oprema, int poslovnicaId,
        DateTime datumOd, DateTime datumDo, int? osimRezervacije, CancellationToken ct = default);

    /// <summary>Oprema koja se uz ovo vozilo moze uzeti u zadatom terminu, sa slobodnom kolicinom.</summary>
    Task<List<DostupnaOpremaDto>> DostupnaZaVoziloAsync(
        int voziloId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default);

    /// <summary>Baca BusinessException kad bi nova kolicina bila manja od vec obecane buducim rezervacijama.</summary>
    Task ObaveznoPokrivaObecanoAsync(
        int poslovnicaId, int vrstaOpremeId, int novaKolicina, CancellationToken ct = default);

    /// <summary>Zakljucava zalihe poslovnice do kraja tekuce transakcije.</summary>
    Task ZakljucajZaliheAsync(int poslovnicaId, CancellationToken ct = default);
}
