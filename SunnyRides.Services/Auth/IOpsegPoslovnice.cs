namespace SunnyRides.Services.Auth;

/// <summary>
/// Na koju poslovnicu je prijavljeni korisnik ogranicen.
///
/// Uposlenik radi u jednoj poslovnici i vidi samo njene rezervacije, vozila, blokade i
/// primopredaje. Administrator vidi sve. Klijent i worker nisu ograniceni po
/// poslovnici - klijenta ogranicava vlasnistvo, a worker radi za cijeli sistem.
///
/// Poslovnica se cita iz baze prema korisniku iz tokena, nikad iz zahtjeva. Da dolazi
/// iz zahtjeva, uposlenik bi je jednostavno promijenio u adresi.
/// </summary>
public interface IOpsegPoslovnice
{
    /// <summary>Poslovnica uposlenika, ili null kad korisnik nije ogranicen.</summary>
    Task<int?> OgranicenjeAsync(CancellationToken ct = default);

    /// <summary>Baca 403 kad je korisnik ogranicen na neku drugu poslovnicu.</summary>
    Task ObaveznoDozvoljenaAsync(int poslovnicaId, CancellationToken ct = default);

    /// <summary>
    /// Isto, za vozilo: baca 404 kad vozilo ne postoji i 403 kad stoji u poslovnici
    /// na koju korisnik nije ogranicen.
    /// </summary>
    Task ObaveznoDozvoljenoVoziloAsync(int voziloId, CancellationToken ct = default);
}
