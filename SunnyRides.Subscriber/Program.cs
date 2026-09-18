using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Database;
using SunnyRides.Services.Placanja;
using SunnyRides.Services.Poruke;
using SunnyRides.Services.Rezervacije;
using SunnyRides.Subscriber.Auth;
using SunnyRides.Subscriber.Email;
using SunnyRides.Subscriber.Obrada;
using SunnyRides.Subscriber.Poruke;
using SunnyRides.Subscriber.Poslovi;

// Tajne se citaju iz istog .env fajla kao i API. U Docker okruzenju varijable
// dolaze iz compose-a, pa fajl ne mora postojati.
var putanjaEnv = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(putanjaEnv))
{
    DotNetEnv.Env.Load(putanjaEnv);
}

var builder = Host.CreateApplicationBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "CONNECTION_STRING nije postavljen. Provjeri .env fajl ili environment varijable.");

// Worker cita i pise istu bazu kao i API, kroz isti DbContext - pravila o tome sta
// se smije upisati ostaju na jednom mjestu.
builder.Services.AddDbContext<SunnyRidesDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton(RabbitMqPostavke.IzOkruzenja());
builder.Services.AddSingleton(EmailPostavke.IzOkruzenja());
builder.Services.AddSingleton<IPosiljalacEmaila, MailKitPosiljalac>();

// Worker i sam objavljuje poruke: periodicni poslovi javljaju o otkazanoj rezervaciji
// i o podsjetniku. Jedna konekcija za cijeli proces, pa je singleton.
builder.Services.AddSingleton<IObjavljivacPoruka, RabbitMqObjavljivac>();

// Poslove pokrece sat, pa prijavljenog korisnika nema. Servisi koji ga traze dobijaju
// prazan odgovor umjesto izmisljenog naloga.
builder.Services.AddSingleton<ICurrentUserService, SistemskiKorisnik>();

// Status rezervacije i ovdje mijenja iskljucivo state machine, ista klasa koju koristi
// API. Automatsko otkazivanje ne smije biti drugo pravilo od rucnog.
builder.Services.AddScoped<IRezervacijaStateMachine, RezervacijaStateMachine>();

DodajStripe(builder.Services);

// DbContext je Scoped, pa je i obrada Scoped: svaka poruka dobija svoj opseg.
builder.Services.AddScoped<ObradaDogadjaja>();

builder.Services.AddHostedService<PotrosacPoruka>();

// Periodicni poslovi. Svaki ima svoj razmak i svoj opseg po prolazu.
builder.Services.AddHostedService<OtkazivanjeNeplacenih>();
builder.Services.AddHostedService<PodsjetniciZaPreuzimanje>();
builder.Services.AddHostedService<PonovnoSlanjePovrata>();
builder.Services.AddHostedService<CiscenjeIsteklihZapisa>();

var host = builder.Build();

var log = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Worker");
var emailPostavke = host.Services.GetRequiredService<EmailPostavke>();

if (!emailPostavke.JeKonfigurisan)
{
    log.LogWarning(
        "SMTP podaci nisu postavljeni. Notifikacije se upisuju u bazu, ali emailovi se nece slati.");
}

log.LogInformation("SunnyRides worker je pokrenut.");

host.Run();

// Stripe klijent workeru treba samo zbog povrata koji nisu stigli do provajdera.
// Kad kljuca nema, klijent se ne pravi - posao tada zapis ostavlja netaknut umjesto
// da ga oznaci kao neuspjeh.
static void DodajStripe(IServiceCollection services)
{
    var postavke = StripePostavke.IzOkruzenja();
    services.AddSingleton(postavke);

    var tajniKljuc = postavke.TajniKljuc;

    services.AddScoped<IStripeKlijent>(sp =>
    {
        var klijent = string.IsNullOrWhiteSpace(tajniKljuc)
            ? null
            : new Stripe.StripeClient(tajniKljuc);

        return new StripeKlijent(postavke, klijent, sp.GetRequiredService<ILogger<StripeKlijent>>());
    });

    services.AddScoped<IIzvrsilacPovrata, IzvrsilacPovrata>();
}
