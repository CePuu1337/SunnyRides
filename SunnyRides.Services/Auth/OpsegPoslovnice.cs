using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.Konstante;
using SunnyRides.Services.Database;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Auth;

public class OpsegPoslovnice : IOpsegPoslovnice
{
    private readonly SunnyRidesDbContext _context;
    private readonly ICurrentUserService _trenutniKorisnik;

    // Servis je Scoped, pa se poslovnica cita najvise jednom po zahtjevu, bez obzira
    // na to koliko je servisa pita.
    private bool _procitano;
    private int? _poslovnicaId;

    public OpsegPoslovnice(SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik)
    {
        _context = context;
        _trenutniKorisnik = trenutniKorisnik;
    }

    public async Task<int?> OgranicenjeAsync(CancellationToken ct = default)
    {
        if (_procitano)
        {
            return _poslovnicaId;
        }

        _poslovnicaId = await ProcitajAsync(ct);
        _procitano = true;

        return _poslovnicaId;
    }

    public async Task ObaveznoDozvoljenaAsync(int poslovnicaId, CancellationToken ct = default)
    {
        var ogranicenje = await OgranicenjeAsync(ct);

        if (ogranicenje.HasValue && ogranicenje.Value != poslovnicaId)
        {
            throw new ForbiddenException(
                "Ovo pripada drugoj poslovnici. Uposlenik radi samo sa svojom poslovnicom.");
        }
    }

    public async Task ObaveznoDozvoljenoVoziloAsync(int voziloId, CancellationToken ct = default)
    {
        var poslovnicaId = await _context.Vozila
            .AsNoTracking()
            .Where(x => x.Id == voziloId)
            .Select(x => (int?)x.PoslovnicaId)
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za("Vozilo", voziloId);

        await ObaveznoDozvoljenaAsync(poslovnicaId, ct);
    }

    private async Task<int?> ProcitajAsync(CancellationToken ct)
    {
        // Administrator nije ogranicen ni kad ima i ulogu uposlenika.
        if (!_trenutniKorisnik.JeUUlozi(Uloge.Uposlenik) || _trenutniKorisnik.JeUUlozi(Uloge.Administrator))
        {
            return null;
        }

        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var poslovnicaId = await _context.Korisnici
            .AsNoTracking()
            .Where(x => x.Id == korisnikId)
            .Select(x => x.PoslovnicaId)
            .FirstOrDefaultAsync(ct);

        // Uposlenik bez poslovnice ne vidi nista, umjesto da vidi sve. Do ovoga ne bi
        // trebalo doci - servis korisnika poslovnicu uposleniku trazi - ali greska u
        // podacima ne smije otvoriti pristup cijeloj agenciji.
        return poslovnicaId
               ?? throw new ForbiddenException(
                   "Vasem nalogu nije dodijeljena poslovnica. Obratite se administratoru.");
    }
}
