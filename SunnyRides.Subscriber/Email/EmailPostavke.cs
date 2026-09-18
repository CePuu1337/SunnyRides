namespace SunnyRides.Subscriber.Email;

/// <summary>
/// SMTP podaci iz .env fajla, procitani jednom pri pokretanju workera.
///
/// Kad nisu postavljeni, worker i dalje radi: notifikacije se upisuju u bazu, a
/// umjesto slanja se u log upise sta bi bilo poslano. Tako se sve ostalo moze
/// testirati i bez SMTP naloga.
/// </summary>
public class EmailPostavke
{
    public string? Host { get; init; }
    public int Port { get; init; }
    public string? Korisnik { get; init; }
    public string? Lozinka { get; init; }
    public string? Posiljalac { get; init; }

    /// <summary>
    /// Kad je postavljena, svaki email ide na ovu adresu umjesto na adresu korisnika.
    ///
    /// Baza je puna izmisljenih ljudi, ali adresa iz demo podataka teoretski moze
    /// pripasti nekom stvarnom. Ova zastita je zato tu kao drugi sloj: demo adrese su
    /// vec na rezervisanim domenama, a ovim se i greskom unesena stvarna adresa
    /// zaustavi prije nego iko dobije poruku koju nije trazio. U pravom radu se ne
    /// postavlja.
    /// </summary>
    public string? PreusmjeriNa { get; init; }

    public bool JeKonfigurisan =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Korisnik)
        && !string.IsNullOrWhiteSpace(Lozinka);

    public static EmailPostavke IzOkruzenja() => new()
    {
        Host = Procitaj("SMTP_HOST"),
        Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587,
        Korisnik = Procitaj("SMTP_USER"),
        Lozinka = Procitaj("SMTP_PASSWORD"),
        Posiljalac = Procitaj("SMTP_FROM") ?? Procitaj("SMTP_USER"),
        PreusmjeriNa = Procitaj("SMTP_PREUSMJERI_NA")
    };

    private static string? Procitaj(string naziv)
    {
        var vrijednost = Environment.GetEnvironmentVariable(naziv)?.Trim();

        return string.IsNullOrWhiteSpace(vrijednost) ? null : vrijednost;
    }
}
