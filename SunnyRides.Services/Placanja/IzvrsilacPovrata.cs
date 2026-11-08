using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Services.Placanja;

public class IzvrsilacPovrata : IIzvrsilacPovrata
{
    private readonly IStripeKlijent _stripe;
    private readonly IObjavljivacPoruka _objavljivac;
    private readonly ILogger<IzvrsilacPovrata> _logger;

    public IzvrsilacPovrata(
        IStripeKlijent stripe, IObjavljivacPoruka objavljivac, ILogger<IzvrsilacPovrata> logger)
    {
        _stripe = stripe;
        _objavljivac = objavljivac;
        _logger = logger;
    }

    /// <summary>
    /// Kljuc je vezan za zapis povrata, pa ponovno slanje istog zapisa nikad ne vrati
    /// novac dvaput. Vrijeme kreiranja je u kljucu jer identifikatori poslije brisanja
    /// baze krecu ispocetka, a Stripe kljuc pamti 24 sata.
    /// </summary>
    public static string KljucPovrata(Refund povrat) => $"povrat-{povrat.Id}-{povrat.DatumKreiranja.Ticks}";

    public async Task<PovratPoruka?> PosaljiAsync(Refund povrat, Placanje placanje, CancellationToken ct)
    {
        if (povrat.Status != StatusPlacanja.Created)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(placanje.ProviderPaymentIntentId))
        {
            povrat.Status = StatusPlacanja.Failed;
            _logger.LogError("Povrat {PovratId} nema PaymentIntent na koji bi se vezao.", povrat.Id);
            return null;
        }

        if (!_stripe.JeKonfigurisan)
        {
            // Ostaje Created - cim kljuc bude postavljen, osoblje ga moze poslati.
            _logger.LogWarning("Povrat {PovratId} nije poslan jer Stripe nije konfigurisan.", povrat.Id);
            return null;
        }

        try
        {
            var odgovor = await _stripe.KreirajPovratAsync(
                placanje.ProviderPaymentIntentId,
                IznosiStripe.UCente(povrat.Iznos),
                KljucPovrata(povrat),
                ct);

            povrat.ProviderRefundId = odgovor.Id;
            povrat.Status = IznosiStripe.StatusPovrata(odgovor.Status);

            _logger.LogInformation(
                "Povrat {PovratId} od {Iznos} EUR poslan Stripe-u kao {RefundId}, status {Status}.",
                povrat.Id, povrat.Iznos, odgovor.Id, odgovor.Status);

            // Stripe moze odgovoriti i statusom "failed" ili "canceled", bez izuzetka.
            // Tada novac nije vracen i klijent ne dobija poruku da jeste - osoblje vidi
            // odbijen povrat na ekranu placanja i moze ga ponoviti.
            if (!IznosiStripe.PovratTrebaJaviti(povrat.Status))
            {
                _logger.LogWarning(
                    "Stripe je povrat {PovratId} vratio sa statusom {Status}; klijent se ne obavjestava.",
                    povrat.Id, odgovor.Status);
                return null;
            }

            return new PovratPoruka(placanje.RezervacijaId, povrat.Id);
        }
        catch (PlatniProvajderException ex) when (ex.Konacna)
        {
            // Stripe je odbio - novac nije vracen. Osoblje vidi Failed i moze pokusati
            // ponovo, sto pravi novi zapis sa novim kljucem.
            povrat.Status = StatusPlacanja.Failed;
            _logger.LogError(ex, "Stripe je odbio povrat {PovratId}.", povrat.Id);
            return null;
        }
        catch (PlatniProvajderException ex)
        {
            // Ishod nepoznat. Zapis ostaje Created, a sljedeci pokusaj ide istim kljucem,
            // pa ako je Stripe povrat ipak izvrsio, vratit ce isti rezultat umjesto novog.
            _logger.LogError(ex, "Povrat {PovratId} nije potvrdjen, ostaje za ponovno slanje.", povrat.Id);
            return null;
        }
    }

    public async Task JaviKlijentuAsync(IEnumerable<PovratPoruka> poruke, CancellationToken ct)
    {
        foreach (var poruka in poruke)
        {
            await _objavljivac.ObjaviAsync(Redovi.PovratIzvrsen, poruka, ct);
        }
    }

    public async Task PonistiIntentAsync(Placanje placanje, CancellationToken ct)
    {
        if (!IznosiStripe.JeOtvoreno(placanje.Status))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(placanje.ProviderPaymentIntentId) || !_stripe.JeKonfigurisan)
        {
            placanje.Status = StatusPlacanja.Canceled;
            placanje.DatumAzuriranja = DateTime.UtcNow;
            return;
        }

        try
        {
            var intent = await _stripe.PonistiIntentAsync(placanje.ProviderPaymentIntentId, ct);

            // Nepostojeci intent se tretira kao ponisten - naplatiti se ionako ne moze.
            placanje.Status = intent is null
                ? StatusPlacanja.Canceled
                : IznosiStripe.StatusIntenta(intent.Status);
            placanje.DatumAzuriranja = DateTime.UtcNow;
        }
        catch (PlatniProvajderException ex)
        {
            // Najcesci razlog: intent je upravo naplacen, pa se vise ne moze ponistiti.
            // Status se ne dira - potvrda ili webhook ce ga zateci kao uspjesan i vratiti
            // novac za rezervaciju koja vise ne vazi.
            _logger.LogWarning(ex,
                "Intent {IntentId} nije ponisten; ostaje na potvrdi ili webhook-u.",
                placanje.ProviderPaymentIntentId);
        }
    }

    public async Task<List<PovratPoruka>> IzvrsiZaRezervacijuAsync(Rezervacija rezervacija, CancellationToken ct)
    {
        var zaKlijenta = new List<PovratPoruka>();

        foreach (var placanje in rezervacija.Placanja.OrderBy(p => p.Id))
        {
            foreach (var povrat in placanje.Refundi.Where(r => r.Status == StatusPlacanja.Created).OrderBy(r => r.Id))
            {
                if (await PosaljiAsync(povrat, placanje, ct) is { } poruka)
                {
                    zaKlijenta.Add(poruka);
                }
            }

            if (rezervacija.Status != StatusRezervacije.Pending)
            {
                await PonistiIntentAsync(placanje, ct);
            }
        }

        return zaKlijenta;
    }
}
