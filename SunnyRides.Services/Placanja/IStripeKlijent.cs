namespace SunnyRides.Services.Placanja;

/// <summary>Stanje PaymentIntent-a kako ga Stripe vidi.</summary>
public record StripeIntent(string Id, string Status, long IznosCenti, long NaplacenoCenti, string? ClientSecret);

/// <summary>Stanje povrata kako ga Stripe vidi.</summary>
public record StripePovrat(string Id, string Status);

/// <summary>Procitan i potpisom provjeren webhook dogadjaj.</summary>
public record StripeDogadjaj(string Id, string Tip, string? PaymentIntentId, string? RefundId, string? RefundStatus);

/// <summary>
/// Jedina klasa koja razgovara sa Stripe SDK-om.
///
/// Servisi rade sa ovim zapisima, a ne sa Stripe tipovima. Tako je komunikacija sa
/// provajderom na jednom mjestu, a servis za placanje sadrzi samo pravila.
/// </summary>
public interface IStripeKlijent
{
    bool JeKonfigurisan { get; }

    Task<StripeIntent> KreirajIntentAsync(
        long iznosCenti, string idempotencyKljuc, int rezervacijaId, string opis, CancellationToken ct);

    /// <summary>Vraca null kad intent kod Stripe-a ne postoji.</summary>
    Task<StripeIntent?> DohvatiIntentAsync(string intentId, CancellationToken ct);

    /// <summary>Vraca null kad intent kod Stripe-a ne postoji.</summary>
    Task<StripeIntent?> PonistiIntentAsync(string intentId, CancellationToken ct);

    Task<StripePovrat> KreirajPovratAsync(
        string intentId, long iznosCenti, string idempotencyKljuc, CancellationToken ct);

    /// <summary>Provjera potpisa bez citanja sadrzaja - koristi je autentifikacija webhook-a.</summary>
    bool PotpisJeIspravan(string json, string potpis);

    /// <summary>Baca <c>BusinessException</c> ako potpis nije ispravan.</summary>
    StripeDogadjaj ProcitajDogadjaj(string json, string potpis);
}
