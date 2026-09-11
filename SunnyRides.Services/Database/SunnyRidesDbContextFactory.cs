using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SunnyRides.Services.Database;

/// <summary>
/// Koristi ga iskljucivo alat `dotnet ef` pri radu sa migracijama.
///
/// Bez ove klase EF bi radi migracija podizao cijeli API host, a time bi se pri
/// svakoj komandi izvrsio i kod koji stoji iza builder.Build() - ukljucujuci
/// migriranje i seed. Ovako EF dobije samo DbContext i nista vise.
/// </summary>
public class SunnyRidesDbContextFactory : IDesignTimeDbContextFactory<SunnyRidesDbContext>
{
    public SunnyRidesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? ProcitajIzEnvFajla("CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "CONNECTION_STRING nije postavljen. Provjeri .env fajl u korijenu repozitorija.");

        var options = new DbContextOptionsBuilder<SunnyRidesDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SunnyRidesDbContext(options);
    }

    /// <summary>
    /// Trazi .env penjuci se od trenutnog foldera prema korijenu repozitorija.
    /// Namjerno bez dodatnog paketa - Services projekat ne treba zavisiti od
    /// biblioteke koja mu treba samo pri radu sa migracijama.
    /// </summary>
    private static string? ProcitajIzEnvFajla(string kljuc)
    {
        var folder = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (folder is not null)
        {
            var putanja = Path.Combine(folder.FullName, ".env");
            if (File.Exists(putanja))
            {
                foreach (var red in File.ReadAllLines(putanja))
                {
                    var linija = red.Trim();
                    if (linija.Length == 0 || linija.StartsWith('#'))
                    {
                        continue;
                    }

                    var razdjelnik = linija.IndexOf('=');
                    if (razdjelnik <= 0)
                    {
                        continue;
                    }

                    if (linija[..razdjelnik].Trim() == kljuc)
                    {
                        return linija[(razdjelnik + 1)..].Trim();
                    }
                }
            }

            folder = folder.Parent;
        }

        return null;
    }
}
