using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SunnyRides.Subscriber.Poslovi;

/// <summary>
/// Zajednicki dio svih poslova koji se ponavljaju po satu.
///
/// Svaki prolaz dobija svoj opseg, jer je DbContext Scoped i ne smije zivjeti koliko
/// i cijeli worker - dugovjecan kontekst bi gomilao zapraceno stanje i radio nad
/// podacima koje je davno procitao.
///
/// Greska u jednom prolazu se biljezi i posao ide dalje. Posao koji padne zbog jedne
/// rezervacije prestao bi obradjivati i sve ostale, a njih niko drugi nece obraditi.
/// </summary>
public abstract class PeriodicniPosao : BackgroundService
{
    private readonly IServiceScopeFactory _fabrikaOpsega;
    private readonly ILogger _logger;

    protected PeriodicniPosao(IServiceScopeFactory fabrikaOpsega, ILogger logger)
    {
        _fabrikaOpsega = fabrikaOpsega;
        _logger = logger;
    }

    /// <summary>Naziv koji se vidi u logu.</summary>
    protected abstract string Naziv { get; }

    protected abstract TimeSpan Razmak { get; }

    /// <summary>
    /// Prvi prolaz ne ide odmah po pokretanju - baza se u Dockeru podize istovremeno
    /// sa workerom i treba joj trenutak.
    /// </summary>
    protected virtual TimeSpan Odgoda => TimeSpan.FromSeconds(20);

    protected abstract Task IzvrsiAsync(IServiceProvider servisi, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Odgoda, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _logger.LogInformation("Posao \"{Naziv}\" se izvrsava svakih {Minuta} min.",
            Naziv, Razmak.TotalMinutes);

        using var tajmer = new PeriodicTimer(Razmak);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var opseg = _fabrikaOpsega.CreateScope();

                await IzvrsiAsync(opseg.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Posao \"{Naziv}\" nije uspio. Pokusavam ponovo u sljedecem prolazu.", Naziv);
            }

            try
            {
                if (!await tajmer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Posao \"{Naziv}\" je zaustavljen.", Naziv);
    }
}
