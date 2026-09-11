namespace SunnyRides.Services.Auth;

/// <summary>
/// Podaci o prijavljenom korisniku, procitani iz JWT tokena.
///
/// Ovo je jedini nacin na koji servis smije saznati ko poziva operaciju. userId se
/// nikad ne prima iz rute, query stringa ni tijela zahtjeva - inace bi klijent
/// mogao poslati tudji identifikator i raditi nad tudjim podacima.
/// </summary>
public interface ICurrentUserService
{
    int? KorisnikId { get; }

    string? KorisnickoIme { get; }

    /// <summary>Identifikator tokena. Pri odjavi se upisuje u tabelu opozvanih tokena.</summary>
    string? Jti { get; }

    DateTime? IsticeUtc { get; }

    IReadOnlyList<string> Uloge { get; }

    bool JeUUlozi(string uloga);

    /// <summary>
    /// Identifikator prijavljenog korisnika, ili izuzetak ako zahtjev nije autentifikovan.
    /// Koristi se tamo gdje operacija bez prijavljenog korisnika nema smisla.
    /// </summary>
    int ObaveznoKorisnikId();
}
