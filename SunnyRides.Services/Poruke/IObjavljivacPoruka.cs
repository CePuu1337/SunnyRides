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
}
