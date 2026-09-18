using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Preporuke;

public class HistorijaPretrageService : IHistorijaPretrageService
{
    private readonly SunnyRidesDbContext _context;
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly ILogger<HistorijaPretrageService> _logger;

    public HistorijaPretrageService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        ILogger<HistorijaPretrageService> logger)
    {
        _context = context;
        _trenutniKorisnik = trenutniKorisnik;
        _logger = logger;
    }

    public async Task ZabiljeziAsync(VoziloSearchObject search, CancellationToken ct = default)
    {
        try
        {
            var korisnikId = _trenutniKorisnik.KorisnikId;

            if (korisnikId is null || !ZasluzujeZapis(search))
            {
                return;
            }

            var gradId = search.GradId ?? await GradPoslovniceAsync(search.PoslovnicaId, ct);

            _context.HistorijaPretraga.Add(new HistorijaPretrage
            {
                KorisnikId = korisnikId.Value,
                TipVozilaId = search.TipVozilaId,
                MarkaId = search.MarkaId,
                GradId = gradId,
                CijenaOd = search.CijenaOd,
                CijenaDo = search.CijenaDo,
                DatumVrijeme = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Zapis o pretrazi je podatak za preporuke, a ne dio odgovora korisniku.
            // Ako upis padne, pretraga mora vratiti rezultate kao da se nista nije
            // desilo - ali greska se biljezi, jer tiho prazna historija znaci da
            // preporuke prestanu raditi a niko ne zna zasto.
            _logger.LogWarning(ex, "Pretraga nije zabiljezena u historiju.");
        }
    }

    /// <summary>
    /// Ne biljezi se svaki dodir liste vozila, nego pretraga koja nesto govori o ukusu.
    ///
    /// Tri slucaja ispadaju:
    ///
    /// 1. **Osoblje.** Administrator i uposlenik pretrazuju flotu zbog posla, a ne zato
    ///    sto biraju vozilo za sebe. Njihovi zapisi bi u profil unijeli tudje namjere.
    /// 2. **Zahtjev bez ijednog filtera.** Otvaranje liste nije pretraga - takav zapis
    ///    bio bi red samih praznih vrijednosti, koji ne moze podici nijedan skor, a
    ///    tabela bi rasla pri svakom otvaranju ekrana.
    /// 3. **Druga i dalje stranice.** Listanje rezultata je ista pretraga, pa bi se
    ///    isti ukus prebrojao onoliko puta koliko korisnik ima strpljenja.
    /// </summary>
    private bool ZasluzujeZapis(VoziloSearchObject search)
    {
        if (_trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik))
        {
            return false;
        }

        if ((search.Page ?? 0) > 0)
        {
            return false;
        }

        return search.TipVozilaId.HasValue
               || search.MarkaId.HasValue
               || search.GradId.HasValue
               || search.PoslovnicaId.HasValue
               || search.CijenaOd.HasValue
               || search.CijenaDo.HasValue;
    }

    /// <summary>
    /// Kad je trazena poslovnica, grad se izvodi iz nje. Profil radi sa gradovima, pa
    /// bi zapis sa poslovnicom a bez grada bio slijep za lokaciju.
    /// </summary>
    private async Task<int?> GradPoslovniceAsync(int? poslovnicaId, CancellationToken ct)
    {
        if (poslovnicaId is null)
        {
            return null;
        }

        return await _context.Poslovnice
            .Where(x => x.Id == poslovnicaId.Value)
            .Select(x => (int?)x.GradId)
            .FirstOrDefaultAsync(ct);
    }
}
