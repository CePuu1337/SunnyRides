using SunnyRides.Services.Auth;

namespace SunnyRides.Subscriber.Auth;

/// <summary>
/// Trenutni korisnik u workeru - a njega nema.
///
/// Worker ne obradjuje niciji zahtjev: poslove pokrece sat, ne covjek. Servisi koji
/// traze prijavljenog korisnika ovdje dobijaju prazan odgovor, pa audit zapis o
/// automatskom otkazivanju ostaje bez izvrsioca. To je tacno stanje stvari i bolje
/// od izmisljenog "sistemskog naloga" koji bi u historiji izgledao kao da je neko
/// stvarno pritisnuo dugme.
///
/// <see cref="ObaveznoKorisnikId"/> baca izuzetak, i tako i treba: ako neki servis u
/// workeru zatrazi identitet, to je greska u kodu, a ne stanje koje se pokriva.
/// </summary>
public class SistemskiKorisnik : ICurrentUserService
{
    public int? KorisnikId => null;

    public string? KorisnickoIme => null;

    public string? Jti => null;

    public DateTime? IsticeUtc => null;

    public IReadOnlyList<string> Uloge => Array.Empty<string>();

    public bool JeUUlozi(string uloga) => false;

    public int ObaveznoKorisnikId() =>
        throw new InvalidOperationException(
            "Posao u workeru nema prijavljenog korisnika. Operacija koja ga trazi ne smije se pozivati odavde.");
}
