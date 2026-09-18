using System.Text;
using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SunnyRides.API.Hubs;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Database;
using SunnyRides.Services.Poruke;

namespace SunnyRides.API.Realtime;

/// <summary>
/// Preuzima obavjestenja sa razmjene i gura ih kroz hub.
///
/// Obavjestenja nastaju u workeru, a otvorene veze prema uredjajima drzi API - dva
/// odvojena procesa. Ovo je spona izmedju njih: worker objavi poruku na razmjenu,
/// API je preuzme i posalje grupi tog korisnika.
///
/// Ovo nije pozadinski posao koji radi posao workera - takav u API projektu ne bi
/// zadovoljio zahtjev za mikroservisom i zato ga ovdje i nema. Posao je vec obavljen
/// u workeru; ovdje se rezultat samo isporucuje, a isporuka mora biti u procesu koji
/// drzi veze, jer se SignalR poruka ne moze poslati izvan njega.
/// </summary>
public class SlusacNotifikacija : BackgroundService
{
    private readonly RabbitMqPostavke _postavke;
    private readonly IHubContext<NotifikacijaHub> _hub;
    private readonly IServiceScopeFactory _fabrikaOpsega;
    private readonly ILogger<SlusacNotifikacija> _logger;

    private IConnection? _konekcija;
    private IChannel? _kanal;

    public SlusacNotifikacija(
        RabbitMqPostavke postavke,
        IHubContext<NotifikacijaHub> hub,
        IServiceScopeFactory fabrikaOpsega,
        ILogger<SlusacNotifikacija> logger)
    {
        _postavke = postavke;
        _hub = hub;
        _fabrikaOpsega = fabrikaOpsega;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PoveziSePonavljajuciAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("API se gasi, prestajem slusati obavjestenja.");
        }
    }

    /// <summary>
    /// Broker i API se u Dockeru pokrecu zajedno, pa se povezivanje ponavlja sa sve
    /// duzim razmakom. Ako broker uopste ne odgovori, API i dalje radi u cijelosti -
    /// obavjestenja tada nisu u realnom vremenu, ali su u bazi i lista ih prikaze.
    /// </summary>
    private async Task PoveziSePonavljajuciAsync(CancellationToken ct)
    {
        var cekanje = TimeSpan.FromSeconds(1);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PoveziSeAsync(ct);
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

    private async Task PoveziSeAsync(CancellationToken ct)
    {
        var fabrika = new ConnectionFactory
        {
            HostName = _postavke.Host,
            Port = _postavke.Port,
            UserName = _postavke.Korisnik,
            Password = _postavke.Lozinka,
            ClientProvidedName = "sunnyrides-api-notifikacije"
        };

        _konekcija = await fabrika.CreateConnectionAsync(ct);
        _kanal = await _konekcija.CreateChannelAsync(cancellationToken: ct);

        await _kanal.ExchangeDeclareAsync(Razmjene.Notifikacije, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: ct);

        // Red bez naziva, privatan i kratkotrajan: broker mu daje ime, vidi ga samo
        // ova konekcija i nestaje kad se ona zatvori. Tako svaka pokrenuta instanca
        // API-ja ima svoj red i dobija kopiju svake poruke, a ugasena instanca ne
        // ostavlja za sobom red u koji se gomilaju poruke koje niko nece procitati.
        var red = await _kanal.QueueDeclareAsync(
            queue: string.Empty, durable: false, exclusive: true, autoDelete: true,
            cancellationToken: ct);

        await _kanal.QueueBindAsync(red.QueueName, Razmjene.Notifikacije, routingKey: string.Empty,
            cancellationToken: ct);

        var potrosac = new AsyncEventingBasicConsumer(_kanal);
        potrosac.ReceivedAsync += async (_, dogadjaj) => await ObradiAsync(dogadjaj, ct);

        // autoAck je ovdje true, za razliku od workera. Poruka nosi isporuku u
        // realnom vremenu i nema je smisla ponavljati: ako je uredjaj u tom trenutku
        // nije primio, obavjestenje ce vidjeti pri sljedecem otvaranju liste.
        await _kanal.BasicConsumeAsync(
            queue: red.QueueName,
            autoAck: true,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: potrosac,
            cancellationToken: ct);

        _logger.LogInformation("Slusam razmjenu {Razmjena} kroz red {Red}.",
            Razmjene.Notifikacije, red.QueueName);
    }

    private async Task ObradiAsync(BasicDeliverEventArgs dogadjaj, CancellationToken ct)
    {
        try
        {
            var tijelo = Encoding.UTF8.GetString(dogadjaj.Body.Span);
            var poruka = JsonSerializer.Deserialize<NotifikacijaPoruka>(tijelo);

            if (poruka is null)
            {
                _logger.LogWarning("Poruka sa razmjene nije u ocekivanom obliku: {Poruka}", tijelo);
                return;
            }

            using var opseg = _fabrikaOpsega.CreateScope();
            var context = opseg.ServiceProvider.GetRequiredService<SunnyRidesDbContext>();

            // Zapis se cita sada, u trenutku isporuke, pa uredjaj dobija tacno ono
            // sto stoji u bazi - isti oblik koji vrati i lista obavjestenja.
            var notifikacija = await context.Notifikacije
                .AsNoTracking()
                .Include(x => x.Rezervacija)
                .FirstOrDefaultAsync(x => x.Id == poruka.NotifikacijaId, ct);

            if (notifikacija is null || notifikacija.KorisnikId != poruka.KorisnikId)
            {
                _logger.LogWarning("Obavjestenje {Id} ne postoji ili ne pripada korisniku {KorisnikId}.",
                    poruka.NotifikacijaId, poruka.KorisnikId);
                return;
            }

            var neprocitanih = await context.Notifikacije
                .CountAsync(x => x.KorisnikId == poruka.KorisnikId && !x.Procitana, ct);

            var grupa = _hub.Clients.Group(NotifikacijaHub.GrupaZa(poruka.KorisnikId));

            // Uz samo obavjestenje salje se i novi broj neprocitanih, da aplikacija
            // ne mora zbog oznake na zvonu raditi jos jedan zahtjev.
            await grupa.SendAsync("NovaNotifikacija", notifikacija.Adapt<NotifikacijaDto>(), ct);
            await grupa.SendAsync("BrojNeprocitanih",
                new BrojNeprocitanihDto { Broj = neprocitanih }, ct);

            _logger.LogInformation("Obavjestenje {Id} poslano korisniku {KorisnikId}.",
                notifikacija.Id, poruka.KorisnikId);
        }
        catch (Exception ex)
        {
            // Greska u isporuci ne smije oboriti slusaoca - sljedeca poruka mora proci.
            // Sadrzaj se biljezi, jer je zapis u bazi ionako vec tu i posao se moze ponoviti.
            _logger.LogError(ex, "Obavjestenje sa razmjene {Razmjena} nije isporuceno.",
                Razmjene.Notifikacije);
        }
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
