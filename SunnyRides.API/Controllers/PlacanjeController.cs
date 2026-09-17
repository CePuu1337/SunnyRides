using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Placanja;

namespace SunnyRides.API.Controllers;

/// <summary>
/// Placanja i povrati.
///
/// Lista i detalj su otvoreni svakom prijavljenom korisniku, a servis suzava rezultat
/// na vlastita placanja kad pita klijent. Nijedan endpoint ne prima iznos ni status -
/// potvrda je samo zahtjev serveru da provjeri stanje kod Stripe-a.
/// </summary>
[Route("api/placanja")]
public class PlacanjeController : BaseController<PlacanjeDto, PlacanjeSearchObject>
{
    private readonly IPlacanjeService _placanjeService;

    public PlacanjeController(IPlacanjeService placanjeService)
        : base(placanjeService)
    {
        _placanjeService = placanjeService;
    }

    /// <summary>
    /// Serverska potvrda naplate. Aplikacija je zove kad PaymentSheet javi uspjeh, ali
    /// taj uspjeh se ne uzima zdravo za gotovo - server pita Stripe.
    /// Ponovljen poziv za potvrdjeno placanje vraca isto stanje, bez ponovnih efekata.
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    public async Task<PlacanjeDto> PotvrdiAsync(int id, CancellationToken ct)
    {
        return await _placanjeService.PotvrdiAsync(id, ct);
    }

    /// <summary>Ponovno slanje povrata koji je Stripe odbio ili na koji nije odgovorio.</summary>
    [HttpPost("povrati/{povratId:int}/ponovi")]
    [Authorize(Roles = Uloge.AdministratorIliUposlenik)]
    public async Task<PlacanjeDto> PonoviPovratAsync(int povratId, CancellationToken ct)
    {
        return await _placanjeService.PonoviPovratAsync(povratId, ct);
    }
}
