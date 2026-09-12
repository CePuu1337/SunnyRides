using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.DTOs;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Flota;

public class SlikaVozilaService : ISlikaVozilaService
{
    /// <summary>Galerija od osam slika je vise nego dovoljna, a sprjecava da jedno vozilo popuni disk.</summary>
    private const int MaksimalnoSlika = 8;

    private readonly SunnyRidesDbContext _context;
    private readonly IPohranaSlika _pohrana;
    private readonly ILogger<SlikaVozilaService> _logger;

    public SlikaVozilaService(
        SunnyRidesDbContext context, IPohranaSlika pohrana, ILogger<SlikaVozilaService> logger)
    {
        _context = context;
        _pohrana = pohrana;
        _logger = logger;
    }

    public async Task<List<SlikaVozilaDto>> ZaVoziloAsync(int voziloId, CancellationToken ct = default)
    {
        await ObaveznoVoziloAsync(voziloId, ct);

        var slike = await _context.SlikeVozila
            .Where(x => x.VoziloId == voziloId)
            .OrderByDescending(x => x.JeGlavna)
            .ThenBy(x => x.Redoslijed)
            .AsNoTracking()
            .ToListAsync(ct);

        return slike.Adapt<List<SlikaVozilaDto>>();
    }

    public async Task<SlikaVozilaDto> DodajAsync(
        int voziloId, Stream sadrzaj, long duzinaBajta, CancellationToken ct = default)
    {
        await ObaveznoVoziloAsync(voziloId, ct);

        var postojece = await _context.SlikeVozila
            .Where(x => x.VoziloId == voziloId)
            .Select(x => new { x.Redoslijed })
            .ToListAsync(ct);

        if (postojece.Count >= MaksimalnoSlika)
        {
            throw new BusinessException(
                $"Vozilo moze imati najvise {MaksimalnoSlika} fotografija. Obrisite neku prije dodavanja nove.");
        }

        // Fajl se snima prije upisa u bazu, jer tek tada znamo putanju koju treba upisati.
        var sacuvana = await _pohrana.SacuvajJavnoAsync(sadrzaj, duzinaBajta, $"vozila/{voziloId}", ct);

        var slika = new SlikaVozila
        {
            VoziloId = voziloId,
            Putanja = sacuvana.Putanja,
            PutanjaThumbnail = sacuvana.PutanjaThumbnail,
            Redoslijed = postojece.Count == 0 ? 0 : postojece.Max(x => x.Redoslijed) + 1,

            // Prva slika automatski postaje glavna, da vozilo nikad ne ostane bez
            // thumbnaila u listi samo zato sto je neko zaboravio kliknuti.
            JeGlavna = postojece.Count == 0
        };

        _context.SlikeVozila.Add(slika);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch
        {
            // Upis u bazu je pao, a fajl je vec na disku. Bez ovoga bi ostao zauvijek,
            // bez ijednog zapisa koji na njega pokazuje.
            _pohrana.ObrisiJavno(sacuvana.Putanja, sacuvana.PutanjaThumbnail);
            throw;
        }

        _logger.LogInformation("Dodana fotografija {SlikaId} za vozilo {VoziloId}.", slika.Id, voziloId);

        return slika.Adapt<SlikaVozilaDto>();
    }

    public async Task<SlikaVozilaDto> PostaviGlavnuAsync(
        int voziloId, int slikaId, CancellationToken ct = default)
    {
        var sveSlike = await _context.SlikeVozila
            .Where(x => x.VoziloId == voziloId)
            .ToListAsync(ct);

        var odabrana = sveSlike.FirstOrDefault(x => x.Id == slikaId)
                       ?? throw NotFoundException.Za("Fotografija vozila", slikaId);

        // Glavna je tacno jedna. Skidanje oznake sa ostalih i postavljanje na odabranu
        // ide u istom SaveChangesAsync pozivu, pa ne postoji trenutak u kojem su dvije
        // glavne ili nijedna.
        foreach (var slika in sveSlike)
        {
            slika.JeGlavna = slika.Id == slikaId;
        }

        await _context.SaveChangesAsync(ct);

        return odabrana.Adapt<SlikaVozilaDto>();
    }

    public async Task ObrisiAsync(int voziloId, int slikaId, CancellationToken ct = default)
    {
        var sveSlike = await _context.SlikeVozila
            .Where(x => x.VoziloId == voziloId)
            .ToListAsync(ct);

        var zaBrisanje = sveSlike.FirstOrDefault(x => x.Id == slikaId)
                         ?? throw NotFoundException.Za("Fotografija vozila", slikaId);

        _context.SlikeVozila.Remove(zaBrisanje);

        // Ako se brise glavna, oznaku preuzima sljedeca po redoslijedu. Inace bi vozilo
        // ostalo sa fotografijama, ali bez thumbnaila u listi.
        if (zaBrisanje.JeGlavna)
        {
            var nasljednica = sveSlike
                .Where(x => x.Id != slikaId)
                .OrderBy(x => x.Redoslijed)
                .FirstOrDefault();

            if (nasljednica is not null)
            {
                nasljednica.JeGlavna = true;
            }
        }

        await _context.SaveChangesAsync(ct);

        // Fajlovi se brisu tek kad je zapis sigurno nestao iz baze. Obrnutim
        // redoslijedom bi neuspjeli SaveChanges ostavio zapis koji pokazuje na fajl
        // kojeg vise nema, a to je gore od suvisnog fajla na disku.
        _pohrana.ObrisiJavno(zaBrisanje.Putanja, zaBrisanje.PutanjaThumbnail);
    }

    private async Task ObaveznoVoziloAsync(int voziloId, CancellationToken ct)
    {
        var postoji = await _context.Vozila.AnyAsync(x => x.Id == voziloId, ct);

        if (!postoji)
        {
            throw NotFoundException.Za("Vozilo", voziloId);
        }
    }
}
