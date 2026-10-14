using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Lokacija;

public class LokacijaKlijenta : ILokacijaKlijenta
{
    private readonly SunnyRidesDbContext _context;
    private readonly ICurrentUserService _trenutniKorisnik;

    public LokacijaKlijenta(SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik)
    {
        _context = context;
        _trenutniKorisnik = trenutniKorisnik;
    }

    public async Task<Polaziste?> PolazisteAsync(
        double? latituda, double? longituda, CancellationToken ct = default)
    {
        Koordinate.ProvjeriPar(latituda, longituda);

        if (latituda is { } sirina && longituda is { } duzina)
        {
            return new Polaziste(sirina, duzina, PoLokaciji: true, GradNaziv: null);
        }

        // Grad u profilu ima samo klijent. Za osoblje se baza i ne pita - desktop
        // aplikacija cita listu vozila stalno, a udaljenost joj ne treba.
        if (_trenutniKorisnik.KorisnikId is not { } korisnikId
            || !_trenutniKorisnik.JeUUlozi(Uloge.Klijent))
        {
            return null;
        }

        var grad = await _context.Korisnici
            .AsNoTracking()
            .Where(x => x.Id == korisnikId
                        && x.Grad != null
                        && x.Grad.Latituda != null
                        && x.Grad.Longituda != null)
            .Select(x => new { x.Grad!.Naziv, x.Grad.Latituda, x.Grad.Longituda })
            .FirstOrDefaultAsync(ct);

        return grad is null
            ? null
            : new Polaziste(grad.Latituda!.Value, grad.Longituda!.Value, PoLokaciji: false, grad.Naziv);
    }

    public async Task<Dictionary<int, double>> UdaljenostiPoslovnicaAsync(
        Polaziste polaziste, CancellationToken ct = default)
    {
        // Poslovnica je nekoliko, pa se citaju samo njihove koordinate, a udaljenost se
        // racuna ovdje - istim racunom koji pokrivaju testovi. Nista se ne filtrira u
        // memoriji: svaka poslovnica sa koordinatama dobije svoju udaljenost.
        var poslovnice = await _context.Poslovnice
            .AsNoTracking()
            .Where(x => x.Latituda != null && x.Longituda != null)
            .Select(x => new { x.Id, x.Latituda, x.Longituda })
            .ToListAsync(ct);

        return poslovnice.ToDictionary(
            x => x.Id,
            x => Udaljenost.Km(polaziste.Latituda, polaziste.Longituda, x.Latituda!.Value, x.Longituda!.Value));
    }
}
