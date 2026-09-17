using System.Net;
using Microsoft.Extensions.Logging;
using Stripe;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Placanja;

public class StripeKlijent : IStripeKlijent
{
    private readonly StripePostavke _postavke;
    private readonly StripeClient? _klijent;
    private readonly ILogger<StripeKlijent> _logger;

    /// <param name="klijent">Prazan kad kljuc nije postavljen - tada svaki poziv prema Stripe-u daje jasnu poruku.</param>
    public StripeKlijent(StripePostavke postavke, StripeClient? klijent, ILogger<StripeKlijent> logger)
    {
        _postavke = postavke;
        _klijent = klijent;
        _logger = logger;
    }

    public bool JeKonfigurisan => _klijent is not null;

    public async Task<StripeIntent> KreirajIntentAsync(
        long iznosCenti, string idempotencyKljuc, int rezervacijaId, string opis, CancellationToken ct)
    {
        var opcije = new PaymentIntentCreateOptions
        {
            Amount = iznosCenti,
            Currency = StripePostavke.Valuta,
            Description = opis,

            // Placanje ostaje u aplikaciji. Nacini placanja koji traze preusmjeravanje
            // na vanjsku stranicu se ne nude, pa PaymentSheet nikad ne otvara preglednik.
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            },

            // Veza sa rezervacijom je vidljiva i u Stripe dashboardu.
            Metadata = new Dictionary<string, string>
            {
                ["rezervacijaId"] = rezervacijaId.ToString()
            }
        };

        try
        {
            var intent = await Servis().PaymentIntents.CreateAsync(
                opcije, new RequestOptions { IdempotencyKey = idempotencyKljuc }, ct);

            return Mapiraj(intent);
        }
        catch (StripeException ex)
        {
            throw Prevedi(ex, "kreiranje PaymentIntent-a", idempotencyKljuc);
        }
    }

    public async Task<StripeIntent?> DohvatiIntentAsync(string intentId, CancellationToken ct)
    {
        try
        {
            var intent = await Servis().PaymentIntents.GetAsync(intentId, cancellationToken: ct);
            return Mapiraj(intent);
        }
        catch (StripeException ex) when (NePostoji(ex))
        {
            _logger.LogWarning("PaymentIntent {IntentId} ne postoji kod Stripe-a.", intentId);
            return null;
        }
        catch (StripeException ex)
        {
            throw Prevedi(ex, "dohvat PaymentIntent-a", intentId);
        }
    }

    public async Task<StripeIntent?> PonistiIntentAsync(string intentId, CancellationToken ct)
    {
        try
        {
            var intent = await Servis().PaymentIntents.CancelAsync(intentId, cancellationToken: ct);
            return Mapiraj(intent);
        }
        catch (StripeException ex) when (NePostoji(ex))
        {
            _logger.LogWarning("PaymentIntent {IntentId} za ponistavanje ne postoji kod Stripe-a.", intentId);
            return null;
        }
        catch (StripeException ex)
        {
            throw Prevedi(ex, "ponistavanje PaymentIntent-a", intentId);
        }
    }

    public async Task<StripePovrat> KreirajPovratAsync(
        string intentId, long iznosCenti, string idempotencyKljuc, CancellationToken ct)
    {
        var opcije = new RefundCreateOptions
        {
            PaymentIntent = intentId,
            Amount = iznosCenti
        };

        try
        {
            var povrat = await Servis().Refunds.CreateAsync(
                opcije, new RequestOptions { IdempotencyKey = idempotencyKljuc }, ct);

            return new StripePovrat(povrat.Id, povrat.Status);
        }
        catch (StripeException ex)
        {
            throw Prevedi(ex, "povrat novca", idempotencyKljuc);
        }
    }

    public bool PotpisJeIspravan(string json, string potpis)
    {
        if (!_postavke.WebhookJeKonfigurisan)
        {
            return false;
        }

        try
        {
            EventUtility.ValidateSignature(json, potpis, _postavke.WebhookTajna!);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Odbijen webhook sa neispravnim potpisom: {Poruka}", ex.Message);
            return false;
        }
    }

    public StripeDogadjaj ProcitajDogadjaj(string json, string potpis)
    {
        if (!_postavke.WebhookJeKonfigurisan)
        {
            throw new BusinessException("Webhook nije konfigurisan: STRIPE_WEBHOOK_SECRET nedostaje u .env fajlu.");
        }

        Event dogadjaj;
        try
        {
            // Verzija API-ja u dogadjaju se ne mora poklapati sa verzijom SDK-a - Stripe
            // CLI salje dogadjaje u verziji naloga. Potpis se i dalje provjerava.
            dogadjaj = EventUtility.ConstructEvent(json, potpis, _postavke.WebhookTajna!,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Webhook odbijen: {Poruka}", ex.Message);
            throw new BusinessException("Potpis webhook zahtjeva nije ispravan.");
        }

        return dogadjaj.Data.Object switch
        {
            PaymentIntent intent => new StripeDogadjaj(dogadjaj.Id, dogadjaj.Type, intent.Id, null, null),
            Stripe.Refund povrat => new StripeDogadjaj(
                dogadjaj.Id, dogadjaj.Type, povrat.PaymentIntentId, povrat.Id, povrat.Status),
            _ => new StripeDogadjaj(dogadjaj.Id, dogadjaj.Type, null, null, null)
        };
    }

    // --- interno -----------------------------------------------------------

    private V1Services Servis() =>
        _klijent?.V1
        ?? throw new BusinessException(
            "Placanje nije konfigurisano: STRIPE_SECRET_KEY nedostaje u .env fajlu.");

    private static StripeIntent Mapiraj(PaymentIntent intent) =>
        new(intent.Id, intent.Status, intent.Amount, intent.AmountReceived, intent.ClientSecret);

    private static bool NePostoji(StripeException ex) =>
        ex.HttpStatusCode == HttpStatusCode.NotFound
        || ex.StripeError?.Code == "resource_missing";

    /// <summary>
    /// Stripe odgovor 4xx znaci da je zahtjev odbijen i da se nije izvrsio. Izuzeci su
    /// 409 (istovremeni zahtjev sa istim kljucem) i 429 (previse zahtjeva) - tu se
    /// moze pokusati ponovo. Bez HTTP statusa (mreza) ishod je nepoznat.
    /// </summary>
    private PlatniProvajderException Prevedi(StripeException ex, string radnja, string oznaka)
    {
        var kod = (int)ex.HttpStatusCode;
        var konacna = kod is >= 400 and < 500 and not 409 and not 429;

        _logger.LogError(ex,
            "Stripe greska pri radnji '{Radnja}' ({Oznaka}): HTTP {Kod}, tip {Tip}, kod {StripeKod}. Konacna: {Konacna}",
            radnja, oznaka, kod, ex.StripeError?.Type, ex.StripeError?.Code, konacna);

        var poruka = konacna
            ? $"Stripe je odbio zahtjev: {ex.StripeError?.Message ?? ex.Message}"
            : "Stripe trenutno nije dostupan. Pokusajte ponovo za minut.";

        return new PlatniProvajderException(poruka, konacna, ex);
    }
}
