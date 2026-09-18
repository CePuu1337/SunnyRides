using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SunnyRides.Model.Poruke;

namespace SunnyRides.Services.Poruke;

/// <summary>
/// Objavljivac sa jednom konekcijom za cijelu aplikaciju.
///
/// Konekcija prema brokeru je skupa - otvaranje TCP veze i rukovanje po svakoj poruci
/// bilo bi sporije od samog posla koji poruka pokrece, a uputstvo (Dodatak A.1) to
/// izricito navodi kao gresku. Zato je ovo singleton koji konekciju otvori pri prvoj
/// poruci i drzi je otvorenom; kanal se otvara po objavi, jer kanali nisu sigurni za
/// istovremeno koristenje iz vise niti.
/// </summary>
public class RabbitMqObjavljivac : IObjavljivacPoruka, IAsyncDisposable
{
    private readonly RabbitMqPostavke _postavke;
    private readonly ILogger<RabbitMqObjavljivac> _logger;
    private readonly SemaphoreSlim _brava = new(1, 1);

    private IConnection? _konekcija;

    public RabbitMqObjavljivac(RabbitMqPostavke postavke, ILogger<RabbitMqObjavljivac> logger)
    {
        _postavke = postavke;
        _logger = logger;
    }

    public async Task ObjaviAsync<T>(string red, T poruka, CancellationToken ct = default)
    {
        try
        {
            var konekcija = await KonekcijaAsync(ct);
            await using var kanal = await konekcija.CreateChannelAsync(cancellationToken: ct);

            // Red se pravi i pri objavi, ne samo u workeru. Tako poruka ima gdje stati
            // i kad worker jos nije pokrenut - inace bi se izgubila.
            await kanal.QueueDeclareAsync(red, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: ct);

            var tijelo = JsonSerializer.SerializeToUtf8Bytes(poruka);

            await kanal.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: red,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",

                    // Poruka prezivljava restart brokera.
                    Persistent = true
                },
                body: tijelo,
                cancellationToken: ct);

            _logger.LogInformation("Poruka objavljena u red {Red}: {Sadrzaj}",
                red, ZaLog(red, Encoding.UTF8.GetString(tijelo)));
        }
        catch (Exception ex)
        {
            // Korisnikov zahtjev je vec uspio i ne obara se zbog emaila koji nije
            // poslan. Greska se biljezi sa sadrzajem poruke, da se zna sta tacno nije
            // stiglo - osim za redove koji nose tajnu.
            _logger.LogError(ex, "Poruka za red {Red} nije objavljena: {Poruka}",
                red, ZaLog(red, JsonSerializer.Serialize(poruka)));
        }
    }

    public async Task ObjaviSvimaAsync<T>(string razmjena, T poruka, CancellationToken ct = default)
    {
        try
        {
            var konekcija = await KonekcijaAsync(ct);
            await using var kanal = await konekcija.CreateChannelAsync(cancellationToken: ct);

            // Razmjena se pravi i pri objavi, iz istog razloga iz kojeg i red: da
            // poruka ima gdje otici i kad nijedan slusalac jos nije pokrenut.
            await kanal.ExchangeDeclareAsync(razmjena, ExchangeType.Fanout, durable: true,
                autoDelete: false, cancellationToken: ct);

            var tijelo = JsonSerializer.SerializeToUtf8Bytes(poruka);

            await kanal.BasicPublishAsync(
                exchange: razmjena,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",

                    // Za razliku od redova, ovdje poruka nije trajna. Ako je ne primi
                    // niko sada, kasnije vise nije zanimljiva - zapis je u bazi.
                    Persistent = false
                },
                body: tijelo,
                cancellationToken: ct);

            _logger.LogDebug("Poruka objavljena na razmjenu {Razmjena}: {Sadrzaj}",
                razmjena, Encoding.UTF8.GetString(tijelo));
        }
        catch (Exception ex)
        {
            // Isto pravilo kao kod redova: posao koji je poruku izazvao je vec obavljen
            // i ne obara se zbog obavjestenja koje nije stiglo na uredjaj.
            _logger.LogError(ex, "Poruka za razmjenu {Razmjena} nije objavljena: {Poruka}",
                razmjena, JsonSerializer.Serialize(poruka));
        }
    }

    /// <summary>Sadrzaj poruke za log, ili napomena umjesto njega kad red nosi tajnu.</summary>
    private static string ZaLog(string red, string sadrzaj) =>
        Redovi.SadrziTajnu(red) ? "<sadrzaj izostavljen>" : sadrzaj;

    private async Task<IConnection> KonekcijaAsync(CancellationToken ct)
    {
        if (_konekcija is { IsOpen: true })
        {
            return _konekcija;
        }

        await _brava.WaitAsync(ct);
        try
        {
            if (_konekcija is { IsOpen: true })
            {
                return _konekcija;
            }

            if (_konekcija is not null)
            {
                await _konekcija.DisposeAsync();
            }

            var fabrika = new ConnectionFactory
            {
                HostName = _postavke.Host,
                Port = _postavke.Port,
                UserName = _postavke.Korisnik,
                Password = _postavke.Lozinka,
                ClientProvidedName = "sunnyrides-api"
            };

            _konekcija = await fabrika.CreateConnectionAsync(ct);

            _logger.LogInformation("Otvorena konekcija prema RabbitMQ-u na {Host}:{Port}.",
                _postavke.Host, _postavke.Port);

            return _konekcija;
        }
        finally
        {
            _brava.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_konekcija is not null)
        {
            await _konekcija.DisposeAsync();
            _konekcija = null;
        }

        _brava.Dispose();
        GC.SuppressFinalize(this);
    }
}
