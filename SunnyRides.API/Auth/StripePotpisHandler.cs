using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SunnyRides.Services.Placanja;

namespace SunnyRides.API.Auth;

/// <summary>
/// Autentifikacija Stripe webhook-a potpisom umjesto JWT-a.
///
/// Stripe ne moze poslati nas token, a endpoint ne smije biti otvoren - uputstvo
/// dozvoljava <c>[AllowAnonymous]</c> iskljucivo na prijavi i registraciji, a
/// POST koji mijenja stanje placanja je upravo ono sto se ne smije ostaviti bez
/// zastite. Zato webhook ima vlastitu shemu: zahtjev je autentifikovan tek kad se
/// potpis iz zaglavlja <c>Stripe-Signature</c> poklopi sa tijelom i tajnom iz .env
/// fajla. Bez toga ASP.NET vraca 401 prije nego zahtjev dodje do kontrolera.
/// </summary>
public class StripePotpisHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Shema = "StripePotpis";

    /// <summary>Uloga koju dobija samo zahtjev sa ispravnim Stripe potpisom.</summary>
    public const string Uloga = "StripeWebhook";

    private const string ZaglavljePotpisa = "Stripe-Signature";

    private readonly IStripeKlijent _stripe;

    public StripePotpisHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IStripeKlijent stripe)
        : base(options, logger, encoder)
    {
        _stripe = stripe;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ZaglavljePotpisa, out var potpis) || string.IsNullOrWhiteSpace(potpis))
        {
            return AuthenticateResult.NoResult();
        }

        // Tijelo se cita ovdje radi provjere potpisa, pa se mora moci procitati jos
        // jednom u kontroleru.
        Request.EnableBuffering();
        string json;
        using (var citac = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            json = await citac.ReadToEndAsync(Context.RequestAborted);
        }
        Request.Body.Position = 0;

        if (!_stripe.PotpisJeIspravan(json, potpis.ToString()))
        {
            return AuthenticateResult.Fail("Potpis Stripe webhook-a nije ispravan.");
        }

        var identitet = new ClaimsIdentity(
            new[]
            {
                new Claim("name", "Stripe"),
                new Claim("role", Uloga)
            },
            Shema,
            nameType: "name",
            roleType: "role");

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identitet), Shema));
    }
}
