using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Database;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Subscriber.Poslovi;

/// <summary>
/// Dan prije preuzimanja klijent dobija podsjetnik sa terminom i poslovnicom.
///
/// Sto je poslano, zna se po notifikaciji koja za tu rezervaciju vec stoji u bazi -
/// obrada poruke je upisuje prije nego sto krene slanje emaila. Zato nije trebala
/// nova kolona u rezervaciji: trag o poslatom podsjetniku vec postoji na mjestu gdje
/// mu je i inace mjesto, u notifikacijama koje klijent vidi u aplikaciji.
/// </summary>
public class PodsjetniciZaPreuzimanje : PeriodicniPosao
{
    /// <summary>Koliko unaprijed se podsjeca.</summary>
    private static readonly TimeSpan Najava = TimeSpan.FromHours(24);

    private const int Serija = 100;

    private readonly ILogger<PodsjetniciZaPreuzimanje> _logger;

    public PodsjetniciZaPreuzimanje(
        IServiceScopeFactory fabrikaOpsega, ILogger<PodsjetniciZaPreuzimanje> logger)
        : base(fabrikaOpsega, logger)
    {
        _logger = logger;
    }

    protected override string Naziv => "podsjetnici za preuzimanje";

    protected override TimeSpan Razmak => TimeSpan.FromMinutes(15);

    protected override async Task IzvrsiAsync(IServiceProvider servisi, CancellationToken ct)
    {
        var context = servisi.GetRequiredService<SunnyRidesDbContext>();
        var objavljivac = servisi.GetRequiredService<IObjavljivacPoruka>();

        var sada = DateTime.UtcNow;
        var granica = sada.Add(Najava);

        var zaPodsjetiti = await context.Rezervacije
            .Where(x => x.Status == StatusRezervacije.Confirmed
                        && x.DatumOd > sada
                        && x.DatumOd <= granica
                        && !context.Notifikacije.Any(n =>
                               n.RezervacijaId == x.Id
                               && n.Tip == TipNotifikacije.PodsjetnikPreuzimanje))
            .OrderBy(x => x.DatumOd)
            .Select(x => x.Id)
            .Take(Serija)
            .ToListAsync(ct);

        if (zaPodsjetiti.Count == 0)
        {
            return;
        }

        foreach (var id in zaPodsjetiti)
        {
            await objavljivac.ObjaviAsync(Redovi.PodsjetnikPreuzimanje, new RezervacijaPoruka(id), ct);
        }

        _logger.LogInformation("Poslano {Broj} podsjetnika za preuzimanje.", zaPodsjetiti.Count);
    }
}
