using SunnyRides.Model.Poruke;
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
///
/// Klijenta ne obavjestava sam. Vraca poruke za povrate koje je Stripe prihvatio, a
/// pozivalac ih objavljuje kroz <see cref="JaviKlijentuAsync"/> tek kad je novi status
/// snimljen. Da poruka ode prije snimanja, worker bi mogao procitati jos stari status.
/// </summary>
public interface IIzvrsilacPovrata
{
    /// <summary>
    /// Salje povrat u statusu <c>Created</c>. Idempotency kljuc je vezan za identifikator
    /// zapisa, pa ponovno slanje istog zapisa nikad ne vrati novac dvaput.
    /// Vraca poruku za klijenta kad je Stripe povrat prihvatio, inace null.
    /// </summary>
    Task<PovratPoruka?> PosaljiAsync(Refund povrat, Placanje placanje, CancellationToken ct);

    /// <summary>Ponistava otvoren intent, da se rezervacija koja vise ne vazi ne moze naplatiti.</summary>
    Task PonistiIntentAsync(Placanje placanje, CancellationToken ct);

    /// <summary>
    /// Salje sve nove povrate i ponistava sve otvorene intente jedne rezervacije.
    /// Vraca poruke za povrate koje je Stripe prihvatio.
    /// </summary>
    Task<List<PovratPoruka>> IzvrsiZaRezervacijuAsync(Rezervacija rezervacija, CancellationToken ct);

    /// <summary>Objavljuje poruke o povratima. Poziva se tek kad je status povrata snimljen.</summary>
    Task JaviKlijentuAsync(IEnumerable<PovratPoruka> poruke, CancellationToken ct);
}
