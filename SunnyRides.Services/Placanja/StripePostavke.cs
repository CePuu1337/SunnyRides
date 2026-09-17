namespace SunnyRides.Services.Placanja;

/// <summary>
/// Kljucevi za Stripe. Citaju se iz okruzenja jednom, pri pokretanju, i registruju
/// kao singleton.
///
/// Aplikacija se podize i bez kljuceva - sve ostalo radi, a endpointi za placanje
/// odgovaraju jasnom porukom sta nedostaje. Pravi (live) kljuc se, s druge strane,
/// odbija odmah: uputstvo trazi sandbox, a slucajno pokretanje sa live kljucem bi
/// znacilo stvarne naplate.
/// </summary>
public class StripePostavke
{
    public const string Valuta = "eur";

    public string? TajniKljuc { get; init; }
    public string? JavniKljuc { get; init; }
    public string? WebhookTajna { get; init; }

    public bool JeKonfigurisan => !string.IsNullOrWhiteSpace(TajniKljuc);

    public bool WebhookJeKonfigurisan => !string.IsNullOrWhiteSpace(WebhookTajna);

    public static StripePostavke IzOkruzenja()
    {
        var tajni = Procitaj("STRIPE_SECRET_KEY");

        if (tajni is not null && !tajni.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "STRIPE_SECRET_KEY mora biti testni kljuc (sk_test_...). Aplikacija radi iskljucivo u Stripe sandbox okruzenju.");
        }

        return new StripePostavke
        {
            TajniKljuc = tajni,
            JavniKljuc = Procitaj("STRIPE_PUBLISHABLE_KEY"),
            WebhookTajna = Procitaj("STRIPE_WEBHOOK_SECRET")
        };
    }

    /// <summary>Prazna vrijednost i vrijednost iz .env.example ("sk_test_...") racunaju se kao nepostavljene.</summary>
    private static string? Procitaj(string naziv)
    {
        var vrijednost = Environment.GetEnvironmentVariable(naziv)?.Trim();

        return string.IsNullOrWhiteSpace(vrijednost) || vrijednost.EndsWith("...", StringComparison.Ordinal)
            ? null
            : vrijednost;
    }
}
