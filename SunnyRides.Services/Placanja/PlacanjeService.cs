using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Rezervacije;

namespace SunnyRides.Services.Placanja;

/// <summary>
/// Naplata rezervacije kroz Stripe.
///
/// Tri puta vode do potvrdjene naplate - potvrda iz aplikacije, webhook i ponovni
/// zahtjev za intent kad je naplata vec prosla - i sva tri zavrsavaju u istoj metodi,
/// <see cref="PrimijeniStanjeAsync"/>. Pravilo o tome sta znaci "placeno" postoji
/// samo jednom.
///
/// Nijedan od tih puteva ne vjeruje klijentu. Iznos se uzima iz rezervacije, a
/// uspjeh se utvrdjuje pitanjem Stripe-u, nikad podatkom koji je aplikacija poslala.
/// </summary>
public class PlacanjeService
    : BaseService<PlacanjeDto, PlacanjeSearchObject, Placanje>, IPlacanjeService
{
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IStripeKlijent _stripe;
    private readonly IIzvrsilacPovrata _izvrsilac;
    private readonly IRezervacijaStateMachine _stateMachine;
    private readonly IAvailabilityService _dostupnost;
    private readonly StripePostavke _postavke;
    private readonly ILogger<PlacanjeService> _logger;

    /// <summary>Kad zahtjev dolazi od klijenta, lista se suzava na njegova placanja.</summary>
    private int? _ogranicenjeNaKorisnika;

    public PlacanjeService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IStripeKlijent stripe,
        IIzvrsilacPovrata izvrsilac,
        IRezervacijaStateMachine stateMachine,
        IAvailabilityService dostupnost,
        StripePostavke postavke,
        ILogger<PlacanjeService> logger)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _stripe = stripe;
        _izvrsilac = izvrsilac;
        _stateMachine = stateMachine;
        _dostupnost = dostupnost;
        _postavke = postavke;
        _logger = logger;
    }

    protected override string NazivEntiteta => "Placanje";

    protected override string PodrazumijevaniPoredak => "DatumKreiranja desc";

    /// <summary>Ishod primjene stanja iz Stripe-a na nase zapise.</summary>
    private enum Ishod
    {
        Potvrdjeno,
        VecObradjeno,
        NijeNaplaceno,
        NeslaganjeIznosa,
        VracenoKlijentu
    }

    // --- citanje -----------------------------------------------------------

    public override async Task<PagedResult<PlacanjeDto>> GetAsync(
        PlacanjeSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaKorisnika = JeOsoblje() ? null : _trenutniKorisnik.ObaveznoKorisnikId();

        return await base.GetAsync(search, ct);
    }

    public override async Task<PlacanjeDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await ProvjeriVlasnistvoAsync(id, ct);

        return await base.GetByIdAsync(id, ct);
    }

    protected override IQueryable<Placanje> AddFilter(PlacanjeSearchObject search, IQueryable<Placanje> upit)
    {
        if (_ogranicenjeNaKorisnika.HasValue)
        {
            upit = upit.Where(x => x.Rezervacija.KorisnikId == _ogranicenjeNaKorisnika.Value);
        }

        if (search.RezervacijaId.HasValue)
        {
            upit = upit.Where(x => x.RezervacijaId == search.RezervacijaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.RezervacijaBroj))
        {
            upit = upit.Where(x => x.Rezervacija.Broj.Contains(search.RezervacijaBroj));
        }

        if (search.Status.HasValue)
        {
            upit = upit.Where(x => x.Status == search.Status.Value);
        }

        if (search.SamoNeuspjeliPovrati == true)
        {
            upit = upit.Where(x => x.Refundi.Any(r => r.Status == StatusPlacanja.Failed));
        }

        return upit;
    }

    /// <summary>Tacno jedna kolekcija - povrati - pa se redovi ne mnoze.</summary>
    protected override IQueryable<Placanje> AddInclude(PlacanjeSearchObject search, IQueryable<Placanje> upit) =>
        SaPovezanim(upit);

    protected override IQueryable<Placanje> AddIncludeDetalji(IQueryable<Placanje> upit) =>
        SaPovezanim(upit);

    private static IQueryable<Placanje> SaPovezanim(IQueryable<Placanje> upit) =>
        upit.Include(x => x.Rezervacija).ThenInclude(r => r.Korisnik)
            .Include(x => x.Refundi).ThenInclude(r => r.KreiraoKorisnik);

    // --- priprema naplate --------------------------------------------------

    public async Task<PlatniIntentDto> KreirajIntentAsync(int rezervacijaId, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        if (!_stripe.JeKonfigurisan)
        {
            throw new BusinessException(
                "Placanje nije konfigurisano: STRIPE_SECRET_KEY nedostaje u .env fajlu.");
        }

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);

        // Dva istovremena zahtjeva za placanje iste rezervacije (dvostruki dodir na
        // dugme) ne smiju napraviti dva intenta - drugi ceka ovdje i zatim vidi prvi.
        await Context.ZakljucajRezervacijuAsync(rezervacijaId, ct);

        var rezervacija = await UcitajRezervacijuAsync(rezervacijaId, ct);

        if (rezervacija.KorisnikId != korisnikId)
        {
            throw new ForbiddenException("Mozete platiti samo svoje rezervacije.");
        }

        var sada = DateTime.UtcNow;
        ProvjeriDaSeMozePlatiti(rezervacija, sada);

        // Iznos je onaj upisan pri kreiranju rezervacije. Ne racuna se ponovo iz
        // cjenovnika: izmjena cjenovnika izmedju kreiranja i placanja ne smije
        // promijeniti cijenu koju je klijent vec prihvatio.
        var ocekivanoCenti = IznosiStripe.UCente(rezervacija.UkupanIznos);

        var otvorena = rezervacija.Placanja
            .Where(p => IznosiStripe.JeOtvoreno(p.Status) && p.ProviderPaymentIntentId != null)
            .OrderByDescending(p => p.Id)
            .ToList();

        foreach (var otvoreno in otvorena)
        {
            var intent = await DohvatiIntentAsync(otvoreno.ProviderPaymentIntentId!, ct);

            if (intent is null || intent.Status == "canceled" || intent.IznosCenti != ocekivanoCenti)
            {
                // Intent koji Stripe ne poznaje, koji je ponisten ili glasi na drugi
                // iznos se ne koristi ponovo. Onaj na pogresan iznos se i ponistava,
                // da ga niko ne moze naplatiti.
                if (intent is not null && intent.Status != "canceled")
                {
                    await _izvrsilac.PonistiIntentAsync(otvoreno, ct);
                }

                otvoreno.Status = StatusPlacanja.Canceled;
                otvoreno.DatumAzuriranja = sada;
                continue;
            }

            if (IznosiStripe.StatusIntenta(intent.Status) == StatusPlacanja.Succeeded)
            {
                // Novac je vec primljen, samo potvrda nije stigla - zavrsava se ovdje.
                await PrimijeniStanjeAsync(rezervacija, otvoreno, intent, sada, ct);
                await ZavrsiAsync(rezervacija, transakcija, ct);

                return NapraviIntentDto(rezervacija, otvoreno, clientSecret: null);
            }

            otvoreno.Status = IznosiStripe.StatusIntenta(intent.Status);
            otvoreno.DatumAzuriranja = sada;

            await SacuvajAsync(ct);
            await transakcija.CommitAsync(ct);

            _logger.LogInformation(
                "Rezervacija {Broj}: ponovo koristen postojeci intent {IntentId}.",
                rezervacija.Broj, intent.Id);

            return NapraviIntentDto(rezervacija, otvoreno, intent.ClientSecret);
        }

        // Kljuc je vezan za rezervaciju i redni broj pokusaja. Ako se zahtjev prema
        // Stripe-u prekine pa ponovi, isti kljuc vraca isti intent umjesto drugog.
        //
        // Vrijeme kreiranja rezervacije je dio kljuca zato sto Stripe kljuc pamti 24
        // sata, a identifikatori krecu od jedinice poslije svakog brisanja baze. Bez
        // toga bi "rez-5-v1" nove baze naletio na "rez-5-v1" stare, sa drugim iznosom,
        // i Stripe bi zahtjev odbio.
        var kljuc = $"rez-{rezervacija.Id}-v{rezervacija.Placanja.Count + 1}-{rezervacija.DatumKreiranja.Ticks}";

        StripeIntent novi;
        try
        {
            novi = await _stripe.KreirajIntentAsync(
                ocekivanoCenti, kljuc, rezervacija.Id, $"SunnyRides rezervacija {rezervacija.Broj}", ct);
        }
        catch (PlatniProvajderException ex)
        {
            throw new BusinessException(ex.Message);
        }

        var placanje = new Placanje
        {
            Iznos = rezervacija.UkupanIznos,
            Valuta = "EUR",
            Status = IznosiStripe.StatusIntenta(novi.Status),
            Provider = "Stripe",
            ProviderPaymentIntentId = novi.Id,
            IdempotencyKey = kljuc,
            DatumKreiranja = sada
        };
        rezervacija.Placanja.Add(placanje);

        await SacuvajAsync(ct);
        await transakcija.CommitAsync(ct);

        _logger.LogInformation(
            "Rezervacija {Broj}: kreiran intent {IntentId} na {Iznos} EUR.",
            rezervacija.Broj, novi.Id, placanje.Iznos);

        return NapraviIntentDto(rezervacija, placanje, novi.ClientSecret);
    }

    private static void ProvjeriDaSeMozePlatiti(Rezervacija rezervacija, DateTime sada)
    {
        if (rezervacija.IsPaid || rezervacija.Placanja.Any(p => p.Status == StatusPlacanja.Succeeded))
        {
            throw new BusinessException("Rezervacija je vec placena.");
        }

        if (rezervacija.Status != StatusRezervacije.Pending)
        {
            throw new BusinessException(
                $"Rezervacija je {PrelaziRezervacije.Naziv(rezervacija.Status)} i vise se ne moze platiti.");
        }

        if (rezervacija.DrziDo is null || rezervacija.DrziDo <= sada)
        {
            throw new BusinessException(
                "Vrijeme za placanje je isteklo i termin je oslobodjen. Napravite novu rezervaciju.");
        }
    }

    // --- potvrda -----------------------------------------------------------

    public async Task<PlacanjeDto> PotvrdiAsync(int placanjeId, CancellationToken ct = default)
    {
        var osnovno = await ProvjeriVlasnistvoAsync(placanjeId, ct);

        // Idempotentnost: potvrdjeno placanje se vraca kakvo jeste. Nema ponovne
        // promjene statusa, novog audit zapisa ni druge notifikacije.
        if (osnovno.Status == StatusPlacanja.Succeeded)
        {
            return await base.GetByIdAsync(placanjeId, ct);
        }

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);
        await Context.ZakljucajRezervacijuAsync(osnovno.RezervacijaId, ct);

        var rezervacija = await UcitajRezervacijuAsync(osnovno.RezervacijaId, ct);
        var placanje = rezervacija.Placanja.First(p => p.Id == placanjeId);

        // Ponovna provjera poslije zakljucavanja: webhook je mogao isto placanje
        // obraditi dok je ovaj zahtjev cekao.
        if (placanje.Status == StatusPlacanja.Succeeded)
        {
            await transakcija.CommitAsync(ct);
            return await base.GetByIdAsync(placanjeId, ct);
        }

        if (string.IsNullOrWhiteSpace(placanje.ProviderPaymentIntentId))
        {
            throw new BusinessException("Placanje nema pripadajuci Stripe intent.");
        }

        var intent = await DohvatiIntentAsync(placanje.ProviderPaymentIntentId, ct);
        var sada = DateTime.UtcNow;

        if (intent is null)
        {
            placanje.Status = StatusPlacanja.Canceled;
            placanje.DatumAzuriranja = sada;
            await SacuvajAsync(ct);
            await transakcija.CommitAsync(ct);

            throw new BusinessException(
                "Ovo placanje ne postoji kod Stripe-a. Pokrenite placanje ponovo iz rezervacije.");
        }

        var ishod = await PrimijeniStanjeAsync(rezervacija, placanje, intent, sada, ct);
        await ZavrsiAsync(rezervacija, transakcija, ct);

        switch (ishod)
        {
            case Ishod.NijeNaplaceno:
                throw new BusinessException(PorukaNijeNaplaceno(intent.Status));

            case Ishod.NeslaganjeIznosa:
                throw new BusinessException(
                    "Iznos koji je Stripe naplatio ne odgovara iznosu rezervacije. " +
                    "Rezervacija nije potvrdjena, a placanje je zadrzano za provjeru osoblja.");
        }

        return await base.GetByIdAsync(placanjeId, ct);
    }

    private static string PorukaNijeNaplaceno(string status) => status switch
    {
        "requires_payment_method" =>
            "Placanje nije izvrseno - kartica jos nije unesena ili je odbijena. " +
            "Mozete pokusati ponovo dok traje drzanje vozila.",
        "requires_action" =>
            "Placanje ceka dodatnu potvrdu banke (3D Secure). Zavrsite potvrdu u aplikaciji.",
        "processing" =>
            "Placanje je u obradi. Rezervacija ce biti potvrdjena cim ga Stripe potvrdi.",
        "canceled" =>
            "Placanje je ponisteno i vise se ne moze naplatiti.",
        _ => $"Placanje jos nije potvrdjeno kod Stripe-a (status: {status})."
    };

    // --- webhook -----------------------------------------------------------

    public async Task ObradiWebhookAsync(string json, string potpis, CancellationToken ct = default)
    {
        var dogadjaj = _stripe.ProcitajDogadjaj(json, potpis);

        // Stripe isti dogadjaj salje i vise puta. Obradjen dogadjaj se prepoznaje po
        // identifikatoru i potvrdjuje bez ikakvih efekata.
        if (await Context.ObradjeniWebhookEventi.AnyAsync(x => x.ProviderEventId == dogadjaj.Id, ct))
        {
            _logger.LogInformation("Webhook {EventId} je vec obradjen, preskacem.", dogadjaj.Id);
            return;
        }

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);

        Rezervacija? rezervacija = null;

        if (dogadjaj.Tip.StartsWith("payment_intent.", StringComparison.Ordinal)
            && dogadjaj.PaymentIntentId is not null)
        {
            rezervacija = await ObradiDogadjajIntentaAsync(dogadjaj, ct);
        }
        else if (dogadjaj.Tip.StartsWith("refund.", StringComparison.Ordinal)
                 && dogadjaj.RefundId is not null)
        {
            var povrat = await Context.Refundi
                .FirstOrDefaultAsync(x => x.ProviderRefundId == dogadjaj.RefundId, ct);

            if (povrat is not null)
            {
                povrat.Status = IznosiStripe.StatusPovrata(dogadjaj.RefundStatus);
            }
        }

        Context.ObradjeniWebhookEventi.Add(new ObradjeniWebhookEvent
        {
            ProviderEventId = dogadjaj.Id,
            TipEventa = dogadjaj.Tip.Length > 100 ? dogadjaj.Tip[..100] : dogadjaj.Tip,
            DatumObrade = DateTime.UtcNow
        });

        try
        {
            await Context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (JeKrsenjeJedinstvenosti(ex))
        {
            var vecObradjen = await Context.ObradjeniWebhookEventi
                .AsNoTracking()
                .AnyAsync(x => x.ProviderEventId == dogadjaj.Id, ct);

            if (!vecObradjen)
            {
                throw;
            }

            // Isti dogadjaj je istovremeno stigao dvaput i drugi primjerak je upisan
            // prvi. Jedinstveni indeks je zadnja linija odbrane - ova transakcija se
            // ponistava, a efekti ostaju samo od onog prvog.
            _logger.LogInformation("Webhook {EventId} je istovremeno obradjen drugim zahtjevom.", dogadjaj.Id);
            return;
        }

        await transakcija.CommitAsync(ct);

        if (rezervacija is not null)
        {
            await IzvrsiKodStripeaAsync(rezervacija, ct);
        }

        _logger.LogInformation("Webhook {EventId} ({Tip}) obradjen.", dogadjaj.Id, dogadjaj.Tip);
    }

    private async Task<Rezervacija?> ObradiDogadjajIntentaAsync(StripeDogadjaj dogadjaj, CancellationToken ct)
    {
        var veza = await Context.Placanja
            .AsNoTracking()
            .Where(x => x.ProviderPaymentIntentId == dogadjaj.PaymentIntentId)
            .Select(x => new { x.Id, x.RezervacijaId })
            .FirstOrDefaultAsync(ct);

        if (veza is null)
        {
            _logger.LogInformation(
                "Webhook {EventId}: intent {IntentId} ne pripada nijednom placanju.",
                dogadjaj.Id, dogadjaj.PaymentIntentId);
            return null;
        }

        await Context.ZakljucajRezervacijuAsync(veza.RezervacijaId, ct);

        var rezervacija = await UcitajRezervacijuAsync(veza.RezervacijaId, ct);
        var placanje = rezervacija.Placanja.First(p => p.Id == veza.Id);

        // Sadrzaj dogadjaja se koristi samo da se zna koje placanje je u pitanju.
        // Stanje se cita iz Stripe-a, jer dogadjaji znaju stici van reda - stariji
        // "payment_failed" poslije novijeg "succeeded".
        var intent = await DohvatiIntentAsync(dogadjaj.PaymentIntentId!, ct);
        if (intent is null)
        {
            return null;
        }

        var ishod = await PrimijeniStanjeAsync(rezervacija, placanje, intent, DateTime.UtcNow, ct);

        _logger.LogInformation(
            "Webhook {EventId}: placanje {PlacanjeId}, ishod {Ishod}.", dogadjaj.Id, placanje.Id, ishod);

        return rezervacija;
    }

    // --- povrat ------------------------------------------------------------

    public async Task<PlacanjeDto> PonoviPovratAsync(int povratId, CancellationToken ct = default)
    {
        var veza = await Context.Refundi
            .AsNoTracking()
            .Where(x => x.Id == povratId)
            .Select(x => new { x.PlacanjeId, x.Placanje.RezervacijaId })
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za("Povrat", povratId);

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);
        await Context.ZakljucajRezervacijuAsync(veza.RezervacijaId, ct);

        var rezervacija = await UcitajRezervacijuAsync(veza.RezervacijaId, ct);
        var placanje = rezervacija.Placanja.First(p => p.Id == veza.PlacanjeId);
        var povrat = placanje.Refundi.First(r => r.Id == povratId);

        Refund zaSlanje;

        switch (povrat.Status)
        {
            case StatusPlacanja.Succeeded:
            case StatusPlacanja.Pending:
                // Vec je kod Stripe-a - ponovno slanje bi bilo dvostruki povrat.
                await transakcija.CommitAsync(ct);
                return await base.GetByIdAsync(placanje.Id, ct);

            case StatusPlacanja.Created:
                // Ishod prethodnog slanja nije poznat. Salje se isti zapis, istim
                // kljucem - ako je Stripe povrat vec izvrsio, vratit ce taj isti.
                zaSlanje = povrat;
                break;

            default:
                // Stripe je povrat odbio, pa novac nije vracen. Pravi se novi zapis sa
                // novim kljucem; odbijeni ostaje u historiji kakav jeste.
                var slobodno = SlobodnoZaPovrat(placanje);
                if (povrat.Iznos > slobodno)
                {
                    throw new BusinessException(
                        $"Povrat od {povrat.Iznos:0.00} EUR premasuje iznos koji je jos moguce vratiti ({slobodno:0.00} EUR).");
                }

                zaSlanje = NoviPovrat(placanje, povrat.Iznos,
                    $"Ponovljen povrat #{povrat.Id}: {povrat.Razlog}", DateTime.UtcNow);
                await SacuvajAsync(ct);
                break;
        }

        await transakcija.CommitAsync(ct);

        await _izvrsilac.PosaljiAsync(zaSlanje, placanje, ct);
        await SacuvajAsync(ct);

        if (zaSlanje.Status == StatusPlacanja.Failed)
        {
            throw new BusinessException(
                "Stripe je ponovo odbio povrat. Razlog je zabiljezen u logu servera.");
        }

        if (zaSlanje.Status == StatusPlacanja.Created)
        {
            throw new BusinessException(
                "Stripe trenutno nije dostupan. Povrat je sacuvan i moze se poslati ponovo.");
        }

        return await base.GetByIdAsync(placanje.Id, ct);
    }

    // --- jezgro ------------------------------------------------------------

    /// <summary>
    /// Primjenjuje stanje iz Stripe-a na placanje i rezervaciju. Jedino mjesto gdje
    /// placanje postaje uspjesno.
    ///
    /// Ne snima - to radi pozivalac, u transakciji koju je otvorio i uz zakljucan red
    /// rezervacije.
    /// </summary>
    private async Task<Ishod> PrimijeniStanjeAsync(
        Rezervacija rezervacija, Placanje placanje, StripeIntent intent, DateTime sada, CancellationToken ct)
    {
        var status = IznosiStripe.StatusIntenta(intent.Status);

        if (status != StatusPlacanja.Succeeded)
        {
            if (placanje.Status != StatusPlacanja.Succeeded && placanje.Status != status)
            {
                placanje.Status = status;
                placanje.DatumAzuriranja = sada;
            }

            return Ishod.NijeNaplaceno;
        }

        if (placanje.Status == StatusPlacanja.Succeeded)
        {
            return Ishod.VecObradjeno;
        }

        if (intent.NaplacenoCenti != IznosiStripe.UCente(placanje.Iznos))
        {
            _logger.LogError(
                "Placanje {PlacanjeId}: Stripe je naplatio {Naplaceno} centi, ocekivano {Ocekivano}. Potvrda odbijena.",
                placanje.Id, intent.NaplacenoCenti, IznosiStripe.UCente(placanje.Iznos));
            return Ishod.NeslaganjeIznosa;
        }

        var naplaceno = IznosiStripe.IzCenti(intent.NaplacenoCenti);
        placanje.NaplaceniIznos = naplaceno;
        placanje.DatumAzuriranja = sada;

        // Rezervacija vec ima uspjesno placanje - ovo je druga naplata istog najma.
        // Status ostaje kakav jeste (jedinstveni indeks dozvoljava samo jedno uspjesno
        // placanje po rezervaciji), a novac se vraca u cijelosti.
        if (rezervacija.Placanja.Any(p => p.Id != placanje.Id && p.Status == StatusPlacanja.Succeeded))
        {
            _logger.LogError(
                "Rezervacija {Broj}: dvostruka naplata kroz intent {IntentId}, pokrecem povrat.",
                rezervacija.Broj, intent.Id);
            NoviPovrat(placanje, naplaceno,
                "Dvostruka naplata iste rezervacije - drugo placanje se vraca u cijelosti.", sada);
            return Ishod.VracenoKlijentu;
        }

        placanje.Status = StatusPlacanja.Succeeded;
        rezervacija.IsPaid = true;

        if (rezervacija.Status != StatusRezervacije.Pending)
        {
            // Novac je stigao za rezervaciju koja vise ne ceka placanje - klijent je
            // otkazao dok je PaymentSheet bio otvoren. Zadrzati ga nema osnova.
            NoviPovrat(placanje, naplaceno,
                "Placanje je stiglo za rezervaciju koja je vec otkazana - vraca se u cijelosti.", sada);
            return Ishod.VracenoKlijentu;
        }

        if (rezervacija.DrziDo is null || rezervacija.DrziDo <= sada)
        {
            // Drzanje je isteklo, pa termin vise nije bio cuvan. Ako ga je u medjuvremenu
            // uzeo neko drugi, potvrditi ovu rezervaciju znacilo bi dvostruki najam.
            await _dostupnost.ZakljucajVoziloAsync(rezervacija.VoziloId, ct);

            var slobodno = await _dostupnost.JeSlobodnoAsync(
                rezervacija.VoziloId, rezervacija.DatumOd, rezervacija.DatumDo, rezervacija.Id, ct);

            if (!slobodno)
            {
                const string razlog =
                    "Placanje je stiglo nakon isteka drzanja, a termin je u medjuvremenu zauzet.";

                _stateMachine.Promijeni(rezervacija, StatusRezervacije.Cancelled,
                    $"Rezervacija otkazana. {razlog} Naplaceni iznos se vraca u cijelosti.", razlog);

                rezervacija.RazlogOtkazivanja = razlog;
                rezervacija.DatumOtkazivanja = sada;
                rezervacija.DrziDo = null;

                NoviPovrat(placanje, naplaceno, razlog, sada);
                return Ishod.VracenoKlijentu;
            }
        }

        _stateMachine.Promijeni(rezervacija, StatusRezervacije.Confirmed,
            $"Placanje verifikovano na serveru. Naplaceno: {naplaceno:0.00} EUR.");

        rezervacija.DrziDo = null;

        return Ishod.Potvrdjeno;
    }

    /// <summary>Snima, potvrdjuje transakciju i tek onda salje Stripe-u ono sto je odluceno.</summary>
    private async Task ZavrsiAsync(
        Rezervacija rezervacija,
        IDbContextTransaction transakcija,
        CancellationToken ct)
    {
        await SacuvajAsync(ct);
        await transakcija.CommitAsync(ct);

        await IzvrsiKodStripeaAsync(rezervacija, ct);
    }

    /// <summary>
    /// Poziv prema Stripe-u ide poslije potvrde transakcije. Odluka je tada vec trajno
    /// upisana, pa ako Stripe ne odgovori, povrat ostaje zapisan i moze se ponoviti -
    /// obrnuti redoslijed bi mogao vratiti novac za odluku koja nije sacuvana.
    /// </summary>
    private async Task IzvrsiKodStripeaAsync(Rezervacija rezervacija, CancellationToken ct)
    {
        await _izvrsilac.IzvrsiZaRezervacijuAsync(rezervacija, ct);
        await SacuvajAsync(ct);
    }

    private Refund NoviPovrat(Placanje placanje, decimal iznos, string razlog, DateTime sada)
    {
        var povrat = new Refund
        {
            Iznos = iznos,
            Razlog = razlog.Length > 500 ? razlog[..500] : razlog,
            Status = StatusPlacanja.Created,

            // Prazno kad je povrat pokrenuo sistem (webhook), a ne prijavljen korisnik.
            KreiraoKorisnikId = _trenutniKorisnik.KorisnikId,
            DatumKreiranja = sada
        };

        placanje.Refundi.Add(povrat);
        return povrat;
    }

    private static decimal SlobodnoZaPovrat(Placanje placanje)
    {
        var vraceno = placanje.Refundi
            .Where(r => IznosiStripe.PovratJeVazeci(r.Status))
            .Sum(r => r.Iznos);

        return (placanje.NaplaceniIznos ?? 0m) - vraceno;
    }

    private async Task<StripeIntent?> DohvatiIntentAsync(string intentId, CancellationToken ct)
    {
        try
        {
            return await _stripe.DohvatiIntentAsync(intentId, ct);
        }
        catch (PlatniProvajderException ex)
        {
            throw new BusinessException(ex.Message);
        }
    }

    private async Task<Rezervacija> UcitajRezervacijuAsync(int rezervacijaId, CancellationToken ct) =>
        await Context.Rezervacije
            .Include(x => x.Placanja).ThenInclude(p => p.Refundi)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == rezervacijaId, ct)
        ?? throw NotFoundException.Za("Rezervacija", rezervacijaId);

    private record OsnovnoOPlacanju(int RezervacijaId, int KorisnikId, StatusPlacanja Status);

    /// <summary>Klijent vidi i potvrdjuje samo svoja placanja. Provjera je prema tokenu.</summary>
    private async Task<OsnovnoOPlacanju> ProvjeriVlasnistvoAsync(int placanjeId, CancellationToken ct)
    {
        var osnovno = await Context.Placanja
            .AsNoTracking()
            .Where(x => x.Id == placanjeId)
            .Select(x => new OsnovnoOPlacanju(x.RezervacijaId, x.Rezervacija.KorisnikId, x.Status))
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za(NazivEntiteta, placanjeId);

        if (!JeOsoblje() && osnovno.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException("Mozete vidjeti samo svoja placanja.");
        }

        return osnovno;
    }

    private PlatniIntentDto NapraviIntentDto(Rezervacija rezervacija, Placanje placanje, string? clientSecret)
    {
        var sada = DateTime.UtcNow;

        return new PlatniIntentDto
        {
            PlacanjeId = placanje.Id,
            RezervacijaId = rezervacija.Id,
            RezervacijaBroj = rezervacija.Broj,
            ClientSecret = placanje.Status == StatusPlacanja.Succeeded ? null : clientSecret,
            PublishableKey = _postavke.JavniKljuc,
            Iznos = placanje.Iznos,
            Valuta = placanje.Valuta,
            Status = placanje.Status,
            IsPaid = rezervacija.IsPaid,
            PreostaloSekundiDrzanja =
                rezervacija.Status == StatusRezervacije.Pending && rezervacija.DrziDo > sada
                    ? (int)(rezervacija.DrziDo.Value - sada).TotalSeconds
                    : null
        };
    }

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);
}
