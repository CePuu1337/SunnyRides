using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunnyRides.Services.Database;

namespace SunnyRides.Subscriber.Poslovi;

/// <summary>
/// Brise zapise koji su odradili svoje.
///
/// Opozvani token se provjerava pri svakom zahtjevu, a njegov posao prestaje u
/// trenutku kad token ionako istekne - od tada ga odbija provjera roka. Da se red ne
/// brise, tabela kroz koju prolazi svaki zahtjev rasla bi zauvijek.
///
/// Iskorisceni i istekli kodovi za reset se brisu iz istog razloga, uz jednodnevnu
/// zadrsku - dovoljno da se u logu vidi sta se desavalo ako se neko zali da mu reset
/// nije radio.
/// </summary>
public class CiscenjeIsteklihZapisa : PeriodicniPosao
{
    private static readonly TimeSpan ZadrskaZaKodove = TimeSpan.FromDays(1);

    private readonly ILogger<CiscenjeIsteklihZapisa> _logger;

    public CiscenjeIsteklihZapisa(
        IServiceScopeFactory fabrikaOpsega, ILogger<CiscenjeIsteklihZapisa> logger)
        : base(fabrikaOpsega, logger)
    {
        _logger = logger;
    }

    protected override string Naziv => "ciscenje isteklih zapisa";

    protected override TimeSpan Razmak => TimeSpan.FromHours(6);

    protected override async Task IzvrsiAsync(IServiceProvider servisi, CancellationToken ct)
    {
        var context = servisi.GetRequiredService<SunnyRidesDbContext>();

        var sada = DateTime.UtcNow;

        // ExecuteDeleteAsync salje jedan DELETE umjesto da ucita redove pa ih brise
        // jedan po jedan.
        var tokeni = await context.OpozvaniTokeni
            .Where(x => x.DatumIsteka < sada)
            .ExecuteDeleteAsync(ct);

        var granicaKodova = sada.Subtract(ZadrskaZaKodove);

        var kodovi = await context.KodoviZaResetLozinke
            .Where(x => x.DatumIsteka < granicaKodova)
            .ExecuteDeleteAsync(ct);

        if (tokeni > 0 || kodovi > 0)
        {
            _logger.LogInformation(
                "Obrisano {Tokeni} isteklih opozvanih tokena i {Kodovi} isteklih kodova za reset.",
                tokeni, kodovi);
        }
    }
}
