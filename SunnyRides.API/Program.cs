using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Seed;

var builder = WebApplication.CreateBuilder(args);

// Tajne se citaju iz .env fajla u korijenu repozitorija.
// U Docker okruzenju varijable dolaze iz docker-compose-a, pa fajl ne mora postojati.
var putanjaEnv = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(putanjaEnv))
{
    DotNetEnv.Env.Load(putanjaEnv);
}

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "CONNECTION_STRING nije postavljen. Provjeri .env fajl ili environment varijable.");

builder.Services.AddDbContext<SunnyRidesDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await PripremiBazuAsync(app);

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();

// Primjenjuje migracije i puni bazu demo podacima pri pokretanju. Zbog toga se
// aplikacija podize sa "docker compose up --build" bez ijedne rucne komande.
// Baza u kontejneru zna trebati dvadesetak sekundi da pocne primati konekcije,
// pa se pokusaj ponavlja sa eksponencijalnim razmakom.
static async Task PripremiBazuAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SunnyRidesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maksimalnoPokusaja = 8;
    var cekanje = TimeSpan.FromSeconds(1);

    for (var pokusaj = 1; pokusaj <= maksimalnoPokusaja; pokusaj++)
    {
        try
        {
            await context.Database.MigrateAsync();
            await new DatabaseSeeder(context).SeedAsync();

            logger.LogInformation("Baza je spremna.");
            return;
        }
        catch (Exception ex) when (pokusaj < maksimalnoPokusaja)
        {
            logger.LogWarning(ex,
                "Baza jos nije dostupna (pokusaj {Pokusaj} od {Ukupno}). Ponavljam za {Sekundi} s.",
                pokusaj, maksimalnoPokusaja, cekanje.TotalSeconds);

            await Task.Delay(cekanje);
            cekanje = TimeSpan.FromSeconds(Math.Min(cekanje.TotalSeconds * 2, 30));
        }
    }
}
