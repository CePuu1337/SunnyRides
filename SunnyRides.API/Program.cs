using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database;

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
