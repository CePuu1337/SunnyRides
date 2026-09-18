using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Poruke;
using SunnyRides.Subscriber.Obrada;

namespace SunnyRides.Subscriber.Poruke;

/// <summary>
/// Slusa sve redove i svaku poruku predaje obradi.
///
/// Consumer je <see cref="AsyncEventingBasicConsumer"/>, kako uputstvo (Dodatak A.1)
/// i trazi - obrada je asinhrona, pa sinhroni consumer ne bi imao gdje cekati posao
/// nego bi blokirao nit brokera.
/// </summary>
public class PotrosacPoruka : BackgroundService
{
    private const int MaksimalnoPokusaja = 4;

    private readonly RabbitMqPostavke _postavke;
    private readonly IServiceScopeFactory _fabrikaOpsega;
    private readonly ILogger<PotrosacPoruka> _logger;

    private IConnection? _konekcija;
    private IChannel? _kanal;

    public PotrosacPoruka(
        RabbitMqPostavke postavke, IServiceScopeFactory fabrikaOpsega, ILogger<PotrosacPoruka> logger)
    {
        _postavke = postavke;
        _fabrikaOpsega = fabrikaOpsega;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PovezinSePonavljajuciAsync(stoppingToken);

        // Posao se dalje odvija u consumeru; ovdje se samo ceka gasenje servisa.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker se gasi, prestajem slusati redove.");
        }
    }

    /// <summary>
    /// Broker zna trebati desetak sekundi da primi konekcije, a worker se u Dockeru
    /// pokrece zajedno sa njim. Zato se povezivanje ponavlja sa sve duzim razmakom
    /// umjesto da servis odmah padne.
    /// </summary>
    private async Task PovezinSePonavljajuciAsync(CancellationToken ct)
    {
        var cekanje = TimeSpan.FromSeconds(1);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PovezinSeAsync(ct);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ jos nije dostupan na {Host}:{Port}. Ponavljam za {Sekundi} s.",
                    _postavke.Host, _postavke.Port, cekanje.TotalSeconds);

                await Task.Delay(cekanje, ct);
                cekanje = TimeSpan.FromSeconds(Math.Min(cekanje.TotalSeconds * 2, 30));
            }
        }
    }

    private async Task PovezinSeAsync(CancellationToken ct)
    {
        var fabrika = new ConnectionFactory
        {
            HostName = _postavke.Host,
            Port = _postavke.Port,
            UserName = _postavke.Korisnik,
            Password = _postavke.Lozinka,
            ClientProvidedName = "sunnyrides-subscriber"
        };

        _konekcija = await fabrika.CreateConnectionAsync(ct);
        _kanal = await _konekcija.CreateChannelAsync(cancellationToken: ct);

        // Jedna poruka po radniku u isto vrijeme. Bez ovoga bi broker odjednom
        // gurnuo sve poruke iz reda, a ovako se sljedeca uzima tek kad je prethodna
        // potvrdjena.
        await _kanal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, ct);

        var potrosac = new AsyncEventingBasicConsumer(_kanal);
        potrosac.ReceivedAsync += async (_, dogadjaj) => await ObradiAsync(dogadjaj, ct);

        foreach (var red in Redovi.Sve)
        {
            await _kanal.QueueDeclareAsync(red, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: ct);

            // autoAck je false: poruka se brise iz reda tek kad je posao stvarno
            // obavljen. Da je true, pad workera bi znacio tiho izgubljen email.
            await _kanal.BasicConsumeAsync(
                queue: red,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: potrosac,
                cancellationToken: ct);

            _logger.LogInformation("Slusam red {Red}.", red);
        }
    }

    /// <summary>
    /// Obrada sa ponavljanjem: 1 s, 2 s, 4 s, 8 s. Ako ni posljednji pokusaj ne
    /// uspije, poruka se odbacuje bez vracanja u red - inace bi se vrtjela u krug i
    /// zaglavila sve iza sebe. Greska se pritom logira sa sadrzajem poruke, pa se
    /// posao moze ponoviti rucno - osim za redove koji nose tajnu.
    /// </summary>
    private async Task ObradiAsync(BasicDeliverEventArgs dogadjaj, CancellationToken ct)
    {
        var red = dogadjaj.RoutingKey;
        var tijelo = Encoding.UTF8.GetString(dogadjaj.Body.Span);
        var cekanje = TimeSpan.FromSeconds(1);

        // Poruka o resetu lozinke nosi kod u citljivom obliku i ne smije u log ni kad
        // obrada padne. Ostale se logiraju u cijelosti, da se posao moze ponoviti rucno.
        var zaLog = Redovi.SadrziTajnu(red) ? "<sadrzaj izostavljen>" : tijelo;

        for (var pokusaj = 1; pokusaj <= MaksimalnoPokusaja; pokusaj++)
        {
            try
            {
                using var opseg = _fabrikaOpsega.CreateScope();
                var obrada = opseg.ServiceProvider.GetRequiredService<ObradaDogadjaja>();

                await obrada.ObradiAsync(red, tijelo, ct);

                await _kanal!.BasicAckAsync(dogadjaj.DeliveryTag, multiple: false, ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Obrada poruke iz reda {Red} nije uspjela (pokusaj {Pokusaj} od {Ukupno}). Poruka: {Poruka}",
                    red, pokusaj, MaksimalnoPokusaja, zaLog);

                if (pokusaj < MaksimalnoPokusaja)
                {
                    await Task.Delay(cekanje, ct);
                    cekanje *= 2;
                }
            }
        }

        _logger.LogError("Poruka iz reda {Red} odbacena poslije {Ukupno} pokusaja: {Poruka}",
            red, MaksimalnoPokusaja, zaLog);

        await _kanal!.BasicNackAsync(dogadjaj.DeliveryTag, multiple: false, requeue: false, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_kanal is not null)
        {
            await _kanal.DisposeAsync();
        }

        if (_konekcija is not null)
        {
            await _konekcija.DisposeAsync();
        }
    }
}
