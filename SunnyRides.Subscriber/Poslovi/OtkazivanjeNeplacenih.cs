using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Placanja;
using SunnyRides.Services.Poruke;
using SunnyRides.Services.Rezervacije;

namespace SunnyRides.Subscriber.Poslovi;

/// <summary>
/// Oslobadja termine rezervacija koje nisu placene u predvidjenom roku.
///
/// Pretraga takvu rezervaciju ionako ne racuna kao zauzece cim <c>DrziDo</c> prodje,
/// pa vozilo nije blokirano ni prije nego posao stigne. Ali rezervacija ne smije
/// ostati vjecno "na cekanju": klijent u svom pregledu mora vidjeti da je istekla, a
/// otvoreni PaymentIntent mora biti ponisten da se termin koji vise ne vazi ne bi
/// naplatio.
/// </summary>
public class OtkazivanjeNeplacenih : PeriodicniPosao
{
    /// <summary>Naziv sistemskog razloga. Po njemu se i pronalazi u sifrarniku.</summary>
    private const string NazivRazloga = "Isteklo vrijeme za placanje";

    /// <summary>Koliko rezervacija najvise po prolazu - da jedan prolaz ne traje predugo.</summary>
    private const int Serija = 50;

    private readonly ILogger<OtkazivanjeNeplacenih> _logger;

    public OtkazivanjeNeplacenih(
        IServiceScopeFactory fabrikaOpsega, ILogger<OtkazivanjeNeplacenih> logger)
        : base(fabrikaOpsega, logger)
    {
        _logger = logger;
    }

    protected override string Naziv => "otkazivanje neplacenih rezervacija";

    protected override TimeSpan Razmak => TimeSpan.FromMinutes(1);

    protected override async Task IzvrsiAsync(IServiceProvider servisi, CancellationToken ct)
    {
        var context = servisi.GetRequiredService<SunnyRidesDbContext>();

        var sada = DateTime.UtcNow;

        var istekle = await context.Rezervacije
            .Where(x => x.Status == StatusRezervacije.Pending && x.DrziDo != null && x.DrziDo <= sada)
            .OrderBy(x => x.DrziDo)
            .Select(x => x.Id)
            .Take(Serija)
            .ToListAsync(ct);

        if (istekle.Count == 0)
        {
            return;
        }

        var razlogId = await RazlogIstekaAsync(context, ct);

        foreach (var id in istekle)
        {
            await OtkaziJednuAsync(servisi, context, id, razlogId, ct);
        }
    }

    private async Task OtkaziJednuAsync(
        IServiceProvider servisi, SunnyRidesDbContext context, int id, int razlogId, CancellationToken ct)
    {
        var stateMachine = servisi.GetRequiredService<IRezervacijaStateMachine>();
        var izvrsilac = servisi.GetRequiredService<IIzvrsilacPovrata>();
        var objavljivac = servisi.GetRequiredService<IObjavljivacPoruka>();

        await using var transakcija = await context.Database.BeginTransactionAsync(ct);

        // Isti red koji zakljucava potvrda placanja. Ako klijent bas u ovom trenutku
        // placa, jedan od njih dvojice ceka - i onaj koji dodje drugi vidi vec
        // promijenjeno stanje umjesto da radi nad starim.
        await context.ZakljucajRezervacijuAsync(id, ct);

        var rezervacija = await context.Rezervacije
            .Include(x => x.Placanja).ThenInclude(p => p.Refundi)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        var sada = DateTime.UtcNow;

        // Stanje se cita ponovo, poslije zakljucavanja. Izmedju pretrage i ovog
        // trenutka placanje je moglo proci.
        if (rezervacija is null
            || rezervacija.Status != StatusRezervacije.Pending
            || rezervacija.DrziDo is null
            || rezervacija.DrziDo > sada)
        {
            await transakcija.RollbackAsync(ct);
            return;
        }

        stateMachine.Promijeni(
            rezervacija,
            StatusRezervacije.Cancelled,
            "Rezervacija je automatski otkazana jer placanje nije zavrseno u predvidjenom roku.",
            NazivRazloga);

        rezervacija.RazlogOtkazivanjaId = razlogId;
        rezervacija.DatumOtkazivanja = sada;
        rezervacija.DrziDo = null;

        // OtkazaoKorisnikId ostaje prazan - nije otkazao ni klijent ni osoblje.

        await context.SaveChangesAsync(ct);
        await transakcija.CommitAsync(ct);

        // Tek kad je otkazivanje trajno upisano. Neplacena rezervacija nema sta da se
        // vraca, ali ima otvoren intent koji od sada ne smije proci.
        await izvrsilac.IzvrsiZaRezervacijuAsync(rezervacija, ct);
        await context.SaveChangesAsync(ct);

        await objavljivac.ObjaviAsync(Redovi.RezervacijaOtkazana, new RezervacijaPoruka(id), ct);

        _logger.LogInformation(
            "Rezervacija {Broj} je otkazana - placanje nije zavrseno na vrijeme.", rezervacija.Broj);
    }

    /// <summary>
    /// Sistemski razlog se ne nudi ni klijentu ni agenciji i nije aktivan, pa ga niko
    /// ne moze izabrati rucno. Postoji samo da otkazana rezervacija ima cime objasniti
    /// zasto je otkazana - i u pregledu i u emailu.
    ///
    /// Ako ga u bazi nema (starija baza, prije ove verzije), upisuje se ovdje. Tako
    /// posao radi i bez ponovnog punjenja baze.
    /// </summary>
    private static async Task<int> RazlogIstekaAsync(SunnyRidesDbContext context, CancellationToken ct)
    {
        var postojeci = await context.RazloziOtkazivanja
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Naziv == NazivRazloga, ct);

        if (postojeci is not null)
        {
            return postojeci.Id;
        }

        var razlog = new RazlogOtkazivanja
        {
            Naziv = NazivRazloga,
            ZaKlijenta = false,
            ZaAgenciju = false,
            TraziNapomenu = false,
            Aktivan = false
        };

        context.RazloziOtkazivanja.Add(razlog);
        await context.SaveChangesAsync(ct);

        return razlog.Id;
    }
}
