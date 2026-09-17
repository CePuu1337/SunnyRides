using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.API.Auth;
using SunnyRides.Services.Placanja;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Prijem Stripe dogadjaja.
///
/// Ovo je dodatni put, a ne glavni: webhook trazi javno dostupnu adresu, koje u
/// Docker okruzenju pri pregledu rada nema. Glavni put je serverska potvrda
/// (<c>POST /api/placanja/{id}/confirm</c>). Oba zavrsavaju u istoj metodi servisa.
///
/// Zasticen je potpisom kroz vlastitu autentifikacijsku shemu, ne JWT-om - vidi
/// <see cref="StripePotpisHandler"/>.
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
[Authorize(AuthenticationSchemes = StripePotpisHandler.Shema, Roles = StripePotpisHandler.Uloga)]
[ApiExplorerSettings(IgnoreApi = true)]
public class StripeWebhookController : ControllerBase
{
    private readonly IPlacanjeService _placanjeService;

    public StripeWebhookController(IPlacanjeService placanjeService)
    {
        _placanjeService = placanjeService;
    }

    [HttpPost]
    public async Task<IActionResult> PrimiAsync(CancellationToken ct)
    {
        using var citac = new StreamReader(Request.Body);
        var json = await citac.ReadToEndAsync(ct);

        await _placanjeService.ObradiWebhookAsync(json, Request.Headers["Stripe-Signature"].ToString(), ct);

        return Ok();
    }
}
