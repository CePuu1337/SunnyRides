using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SunnyRides.API.Auth;
using SunnyRides.API.Extensions;
using SunnyRides.API.Filters;
using SunnyRides.API.Middleware;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Seed;
using SunnyRides.Services.Fajlovi;
using SunnyRides.Services.Mapping;

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
{
    options.UseSqlServer(connectionString);

    // Krsenje jedinstvenog indeksa je ocekivan ishod, ne kvar sistema - servis ga
    // hvata i pretvara u razumljivu poruku sa statusom 400. Bez ove linije EF isti
    // dogadjaj prijavljuje kao Error sa punim stack traceom, pa u logu izgleda kao
    // da je aplikacija pala, iako je uredno odgovorila. Dogadjaj se i dalje biljezi,
    // samo na Debug nivou.
    options.ConfigureWarnings(w => w.Log((CoreEventId.SaveChangesFailed, LogLevel.Debug)));
});

// Kes za sifrarnike i cjenovnik - podaci koji se citaju pri svakoj pretrazi,
// a mijenjaju rijetko. Na servisnom nivou, ne kao Dictionary u servisu.
builder.Services.AddMemoryCache();

builder.Services.AddHttpContextAccessor();

var jwtPostavke = JwtPostavke.IzOkruzenja();

// Folderi za otpremljene fajlove se razrjesavaju jednom, pri pokretanju, i provjeri
// se da postoje. Ako ih nema, bolje da aplikacija to javi odmah nego pri prvom uploadu.
var pohranaOpcije = PohranaOpcije.IzOkruzenja();

builder.Services.DodajServise(jwtPostavke, pohranaOpcije);

MapsterKonfiguracija.Registruj();

// Bez ovoga bi se kratki nazivi claimova pri citanju prevodili u duge URI oblike
// (npr. "role" u ".../claims/role"), pa se RoleClaimType ispod ne bi poklopio.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Token putuje preko mreze, pa se provjerava sve sto se moze provjeriti:
        // potpis (da ga nije neko drugi izdao), izdavalac i primalac (da nije token
        // iz drugog sistema) i rok trajanja.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtPostavke.Kljuc)),

            ValidateIssuer = true,
            ValidIssuer = jwtPostavke.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtPostavke.Audience,

            ValidateLifetime = true,

            // Podrazumijevana tolerancija je pet minuta, sto znaci da istekao token
            // jos pet minuta prolazi. Za ovaj sistem to nema smisla.
            ClockSkew = TimeSpan.Zero,

            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    // Jedno mjesto koje pretvara izuzetke u HTTP odgovore.
    options.Filters.Add<ExceptionFilter>();
});

// Greske iz anotacija i greske iz servisa moraju izgledati isto.
//
// [ApiController] po defaultu vraca ValidationProblemDetails sa recnikom "errors",
// dok ExceptionFilter vraca ProblemDetails sa poljem "detail". To su dva oblika istog
// dogadjaja - zahtjev nije prihvacen - i klijentska aplikacija bi za svaki morala
// imati vlastito citanje. Uputstvo trazi da se backend poruka proslijedi korisniku, a
// to je lakse ispuniti kad postoji samo jedan oblik.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var poruke = context.ModelState
            .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(greska => greska.ErrorMessage))
            .Where(poruka => !string.IsNullOrWhiteSpace(poruka))
            .Distinct()
            .ToList();

        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Zahtjev nije prihvacen",
            Detail = poruke.Count > 0
                ? string.Join(" ", poruke)
                : "Zahtjev sadrzi neispravne podatke.",
            Instance = context.HttpContext.Request.Path
        });
    };
});

// CORS se konfigurise jednom, sa izricito navedenim origin-ima.
const string CorsPolitika = "SunnyRidesCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolitika, policy => policy
        .WithOrigins(
            "http://localhost:5000",
            "http://localhost:3000",
            "http://10.0.2.2:5000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Swagger ide kroz Swashbuckle, a ne kroz ugradjeni AddOpenApi(), jer nam treba
// Swagger UI sa dugmetom za unos Bearer tokena.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SunnyRides API",
        Version = "v1",
        Description = "Sistem za rezervaciju i najam skutera, motocikala i quadova."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Unesite token dobijen na /api/auth/login. Prefiks \"Bearer\" se dodaje sam."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await PripremiBazuAsync(app);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolitika);

// Fotografije vozila i obavijesti se posluzuju kao obicni staticki fajlovi, bez
// tokena. To su katalog i oglasi agencije - isti sadrzaj za svakoga.
//
// Posluzuje se iskljucivo javni korijen. Fotografije vozackih dozvola i stete zive
// u odvojenom folderu koji ovdje nije naveden i do kojeg se dolazi samo kroz
// endpoint sa provjerom vlasnistva. Da su u istom stablu, ovaj jedan poziv bio bi
// dovoljan da postanu javne.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pohranaOpcije.JavniKorijen),
    RequestPath = PohranaOpcije.JavniPrefiks,

    // Fajl nepoznatog tipa se ne posluzuje. Bez ovoga bi sve sto zavrsi u folderu
    // bilo dostupno za preuzimanje, bez obzira na to sta je.
    ServeUnknownFileTypes = false
});

// Redoslijed je bitan i nije proizvoljan:
// 1. UseAuthentication popunjava HttpContext.User iz tokena.
// 2. Middleware za opozvane tokene tada vec zna koji je jti u pitanju i moze
//    odbiti token koji je odjavom ponisten prije isteka roka.
// 3. UseAuthorization tek onda provjerava [Authorize] i uloge.
// Kad bi provjera opoziva isla prije autentifikacije, ne bi imala sta citati.
app.UseAuthentication();
app.UseMiddleware<OpozvaniTokenMiddleware>();
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
