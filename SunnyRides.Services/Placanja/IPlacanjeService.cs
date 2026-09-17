using SunnyRides.Model.DTOs;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Placanja;

public interface IPlacanjeService : IService<PlacanjeDto, PlacanjeSearchObject>
{
    /// <summary>
    /// Priprema naplatu rezervacije. Iznos uzima iz rezervacije, nikad iz zahtjeva.
    /// Ako vec postoji otvoren intent, vraca njega umjesto novog.
    /// </summary>
    Task<PlatniIntentDto> KreirajIntentAsync(int rezervacijaId, CancellationToken ct = default);

    /// <summary>
    /// Serverska potvrda: pita Stripe za stvarno stanje i tek tada potvrdjuje
    /// rezervaciju. Idempotentna - uspjesno placanje se vraca bez ponovnih efekata.
    /// </summary>
    Task<PlacanjeDto> PotvrdiAsync(int placanjeId, CancellationToken ct = default);

    /// <summary>Dodatni put do istog ishoda, kad Stripe sam javi promjenu.</summary>
    Task ObradiWebhookAsync(string json, string potpis, CancellationToken ct = default);

    /// <summary>Ponovno slanje povrata koji Stripe nije prihvatio. Samo za osoblje.</summary>
    Task<PlacanjeDto> PonoviPovratAsync(int povratId, CancellationToken ct = default);
}
