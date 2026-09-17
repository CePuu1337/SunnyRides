using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Placanja;

/// <summary>
/// Izvrsava kod Stripe-a ono sto je u bazi vec odluceno: salje povrate i ponistava
/// intente koji vise ne smiju biti naplaceni.
///
/// Mijenja samo entitete koje dobije i **ne** poziva <c>SaveChangesAsync</c>. Snima
/// servis koji vodi operaciju, nad istim <c>DbContext</c>-om - tako nema servisa koji
/// iz drugog servisa sam snima, a upis ima jednog vlasnika.
///
/// Namjerno ne baca izuzetke. Poziva se poslije vec potvrdjene transakcije (otkazivanje
/// je zavrseno i kad Stripe ne odgovori), pa neuspjeh ostaje zapisan u statusu povrata
/// i vidljiv osoblju, umjesto da obori zahtjev koji je u sustini uspio.
/// </summary>
public interface IIzvrsilacPovrata
{
    /// <summary>
    /// Salje povrat u statusu <c>Created</c>. Idempotency kljuc je vezan za identifikator
    /// zapisa, pa ponovno slanje istog zapisa nikad ne vrati novac dvaput.
    /// </summary>
    Task PosaljiAsync(Refund povrat, Placanje placanje, CancellationToken ct);

    /// <summary>Ponistava otvoren intent, da se rezervacija koja vise ne vazi ne moze naplatiti.</summary>
    Task PonistiIntentAsync(Placanje placanje, CancellationToken ct);

    /// <summary>Salje sve nove povrate i ponistava sve otvorene intente jedne rezervacije.</summary>
    Task IzvrsiZaRezervacijuAsync(Rezervacija rezervacija, CancellationToken ct);
}
