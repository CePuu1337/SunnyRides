namespace SunnyRides.Services.Poruke;

/// <summary>
/// Slanje poruka u red.
///
/// Objava se **nikad** ne radi unutar transakcije nego tek kad je upis potvrdjen -
/// poruka o placanju koje je u medjuvremenu poništeno gora je od poruke koja kasni.
/// Implementacija namjerno ne baca izuzetke: ako je broker nedostupan, zahtjev
/// korisnika je vec uspio i ne smije pasti zbog emaila koji nije poslan.
/// </summary>
public interface IObjavljivacPoruka
{
    Task ObjaviAsync<T>(string red, T poruka, CancellationToken ct = default);

    /// <summary>
    /// Objava na razmjenu, za poruke koje treba da dobiju svi slusaoci umjesto samo
    /// jedan. Koristi se za guranje obavjestenja u aplikaciju: svaka pokrenuta
    /// instanca API-ja drzi svoje veze prema uredjajima, pa svaka mora dobiti kopiju.
    ///
    /// Poruka namjerno ne preživljava restart brokera. Guranje u realnom vremenu ima
    /// smisla samo dok je dogadjaj svjez - zapis je ionako vec u bazi i aplikacija ga
    /// pokupi pri sljedecem otvaranju liste.
    /// </summary>
    Task ObjaviSvimaAsync<T>(string razmjena, T poruka, CancellationToken ct = default);
}
