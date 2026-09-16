using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Database.Seed;

/// <summary>
/// Puni bazu demo podacima pri prvom pokretanju. Ako u bazi vec postoji ijedan
/// korisnik, seeder ne radi nista - tako se ponovno pokretanje aplikacije ne
/// pretvara u dupliranje podataka.
///
/// Seed je runtime, a ne HasData, zbog obima: preko hiljadu zapisa sa medjusobnim
/// vezama i datumima racunatim od danasnjeg dana. Uputstvo to izricito dozvoljava
/// ("podaci se mogu kreirati i prilikom pokretanja aplikacije").
/// </summary>
public partial class DatabaseSeeder
{
    private readonly SunnyRidesDbContext _context;

    /// <summary>Fiksno sjeme - svako pokretanje daje isti raspored podataka.</summary>
    private readonly Random _rnd = new(220182);

    /// <summary>Sve sto se racuna od danas racuna se od ove vrijednosti.</summary>
    private readonly DateTime _danas = DateTime.UtcNow.Date;

    /// <summary>
    /// Pohrana je opciona jer je seeder koristi samo za jednu stvar - placeholder
    /// fotografiju vozacke dozvole. Bez nje seed radi normalno, samo dozvole ostanu
    /// bez slike.
    /// </summary>
    private readonly IPohranaSlika? _pohrana;

    public DatabaseSeeder(SunnyRidesDbContext context, IPohranaSlika? pohrana = null)
    {
        _context = context;
        _pohrana = pohrana;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _context.Korisnici.AnyAsync(ct))
        {
            return;
        }

        await using var transakcija = await _context.Database.BeginTransactionAsync(ct);

        await SeedSifrarniciAsync(ct);
        await SeedKorisniciAsync(ct);
        await SeedFlotaAsync(ct);
        await SeedPoslovanjeAsync(ct);

        await transakcija.CommitAsync(ct);
    }

    // --- pomocne metode -------------------------------------------------

    private T Izaberi<T>(IReadOnlyList<T> stavke) => stavke[_rnd.Next(stavke.Count)];

    private int Broj(int odUkljucivo, int doIskljucivo) => _rnd.Next(odUkljucivo, doIskljucivo);

    private bool Sansa(int procenat) => _rnd.Next(100) < procenat;

    private decimal Zaokruzi(decimal iznos) => Math.Round(iznos, 2, MidpointRounding.AwayFromZero);
}
