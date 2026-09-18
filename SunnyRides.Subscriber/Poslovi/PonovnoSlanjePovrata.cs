using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database;
using SunnyRides.Services.Placanja;

namespace SunnyRides.Subscriber.Poslovi;

/// <summary>
/// Salje povrate koji su zapisani, a nisu stigli do Stripe-a.
///
/// Povrat se u bazu upisuje unutar transakcije otkazivanja, a Stripe-u salje tek
/// poslije potvrde - ako mreza pukne bas tada, otkazivanje ostaje vazece, a povrat
/// stoji u statusu <c>Created</c>. Bez ovog posla bi cekao da ga neko primijeti.
///
/// Ponovno slanje ide istim idempotency kljucem, koji je vezan za zapis povrata. Ako
/// je Stripe prvi zahtjev ipak primio, vratice isti povrat umjesto da napravi novi -
/// pa se novac ne moze vratiti dvaput.
/// </summary>
public class PonovnoSlanjePovrata : PeriodicniPosao
{
    /// <summary>
    /// Koliko star povrat mora biti da bi se dirao. Mladji je vjerovatno upravo u
    /// slanju iz API-ja i ne treba mu pomoc.
    /// </summary>
    private static readonly TimeSpan Zrelost = TimeSpan.FromMinutes(2);

    private const int Serija = 25;

    private readonly ILogger<PonovnoSlanjePovrata> _logger;

    public PonovnoSlanjePovrata(
        IServiceScopeFactory fabrikaOpsega, ILogger<PonovnoSlanjePovrata> logger)
        : base(fabrikaOpsega, logger)
    {
        _logger = logger;
    }

    protected override string Naziv => "ponovno slanje povrata";

    protected override TimeSpan Razmak => TimeSpan.FromMinutes(5);

    protected override async Task IzvrsiAsync(IServiceProvider servisi, CancellationToken ct)
    {
        var context = servisi.GetRequiredService<SunnyRidesDbContext>();
        var izvrsilac = servisi.GetRequiredService<IIzvrsilacPovrata>();

        var granica = DateTime.UtcNow.Subtract(Zrelost);

        var zaostali = await context.Refundi
            .Include(x => x.Placanje)
            .Where(x => x.Status == StatusPlacanja.Created && x.DatumKreiranja <= granica)
            .OrderBy(x => x.Id)
            .Take(Serija)
            .ToListAsync(ct);

        if (zaostali.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Pronadjeno {Broj} povrata koji jos nisu poslani.", zaostali.Count);

        foreach (var povrat in zaostali)
        {
            // Izvrsilac ne baca izuzetke i ne snima sam; status mijenja na entitetu.
            await izvrsilac.PosaljiAsync(povrat, povrat.Placanje, ct);
        }

        await context.SaveChangesAsync(ct);
    }
}
