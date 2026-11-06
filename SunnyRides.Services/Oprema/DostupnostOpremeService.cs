using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Database;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Vrijeme;

namespace SunnyRides.Services.Oprema;

/// <summary>
/// Slobodna oprema = zalihe poslovnice minus oprema koju u istom terminu vec drze
/// druge rezervacije te poslovnice.
///
/// Opremu drze iste rezervacije koje drze i vozilo: potvrdjene, i neplacene dok im
/// traje drzanje termina. Otkazana i zavrsena ne drze nista. Buffer za pripremu se
/// ovdje ne dodaje - on postoji zbog pranja i tocenja vozila, a kaciga koja je
/// vracena u podne moze istog popodneva ici dalje.
/// </summary>
public class DostupnostOpremeService : IDostupnostOpremeService
{
    /// <summary>Gornja granica liste; katalog opreme je ionako desetak stavki.</summary>
    private const int NajviseVrsta = 100;

    private readonly SunnyRidesDbContext _context;

    public DostupnostOpremeService(SunnyRidesDbContext context)
    {
        _context = context;
    }

    public async Task ObaveznoDostupnoAsync(
        IReadOnlyCollection<StavkaOpremeRequest> oprema, int poslovnicaId,
        DateTime datumOd, DateTime datumDo, int? osimRezervacije, CancellationToken ct = default)
    {
        // Ista vrsta poslana dvaput je jedna stavka sa zbirom - inace bi svaka
        // posebno prosla provjeru, a zajedno ne bi.
        var trazeno = oprema
            .Where(x => x.Kolicina > 0)
            .GroupBy(x => x.VrstaOpremeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Kolicina));

        if (trazeno.Count == 0)
        {
            return;
        }

        // Zakljucavanje prije citanja, iz istog razloga kao kod vozila: dvije
        // rezervacije razlicitih vozila mogu u istom trenutku traziti posljednju
        // kacigu. Bez zakljucavanja obje vide jednu slobodnu i obje je uzmu.
        await ZakljucajZaliheAsync(poslovnicaId, ct);

        var vrste = trazeno.Keys.ToList();

        var nazivi = await _context.VrsteOpreme
            .Where(x => vrste.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Naziv, ct);

        var naStanju = await _context.StanjaOpreme
            .Where(x => x.PoslovnicaId == poslovnicaId && vrste.Contains(x.VrstaOpremeId))
            .ToDictionaryAsync(x => x.VrstaOpremeId, x => x.Kolicina, ct);

        var poslovnica = await _context.Poslovnice
            .Where(x => x.Id == poslovnicaId)
            .Select(x => x.Naziv)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var zauzeto = await ZauzetoAsync(poslovnicaId, vrste, datumOd, datumDo, osimRezervacije, ct);

        foreach (var (vrstaId, kolicina) in trazeno)
        {
            if (!nazivi.TryGetValue(vrstaId, out var naziv))
            {
                throw new BusinessException($"Oprema sa identifikatorom {vrstaId} ne postoji.");
            }

            var razlog = IstovremenaOprema.Razlog(
                naziv,
                poslovnica,
                naStanju.GetValueOrDefault(vrstaId),
                zauzeto.GetValueOrDefault(vrstaId).Kolicina,
                kolicina);

            if (razlog is not null)
            {
                throw new BusinessException(razlog);
            }
        }
    }

    public async Task<List<DostupnaOpremaDto>> DostupnaZaVoziloAsync(
        int voziloId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default)
    {
        if (datumDo <= datumOd)
        {
            throw new BusinessException("Datum vracanja mora biti poslije datuma preuzimanja.");
        }

        // Poslovnica se uzima sa vozila, ne iz zahtjeva: oprema se preuzima tamo
        // gdje se preuzima i vozilo.
        var poslovnicaId = await _context.Vozila
            .Where(x => x.Id == voziloId)
            .Select(x => (int?)x.PoslovnicaId)
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za("Vozilo", voziloId);

        var zalihe = await _context.StanjaOpreme
            .Where(x => x.PoslovnicaId == poslovnicaId && x.Kolicina > 0)
            .OrderBy(x => x.VrstaOpreme.Naziv)
            .Take(NajviseVrsta)
            .Select(x => new DostupnaOpremaDto
            {
                Id = x.VrstaOpremeId,
                Naziv = x.VrstaOpreme.Naziv,
                CijenaPoDanu = x.VrstaOpreme.CijenaPoDanu,
                FiksnaCijena = x.VrstaOpreme.FiksnaCijena,
                Slobodno = x.Kolicina
            })
            .ToListAsync(ct);

        var zauzeto = await ZauzetoAsync(poslovnicaId, null, datumOd, datumDo, null, ct);

        foreach (var stavka in zalihe)
        {
            stavka.Slobodno -= zauzeto.GetValueOrDefault(stavka.Id).Kolicina;
        }

        // Oprema bez slobodnih komada se ne nudi. Odluka je na serveru, kao i kod
        // vozila - aplikacija prikazuje ono sto dobije.
        return zalihe.Where(x => x.Slobodno > 0).ToList();
    }

    public async Task ObaveznoPokrivaObecanoAsync(
        int poslovnicaId, int vrstaOpremeId, int novaKolicina, CancellationToken ct = default)
    {
        // Gleda se sve sto jos nije vraceno: buduce rezervacije i najmovi koji
        // upravo traju. Proslost se ne racuna - ta oprema je vec vracena.
        var obecano = await ZauzetoAsync(
            poslovnicaId, new[] { vrstaOpremeId }, DateTime.UtcNow, DateTime.MaxValue, null, ct);

        var vrhunac = obecano.GetValueOrDefault(vrstaOpremeId);

        if (novaKolicina >= vrhunac.Kolicina)
        {
            return;
        }

        var naziv = await _context.VrsteOpreme
            .Where(x => x.Id == vrstaOpremeId)
            .Select(x => x.Naziv)
            .FirstOrDefaultAsync(ct);

        var od = vrhunac.Od is { } trenutak ? $" (od {LokalnoVrijeme.Datum(trenutak)})" : string.Empty;

        throw new BusinessException(
            $"Rezervacije u ovoj poslovnici vec drze {vrhunac.Kolicina} kom. opreme \"{naziv}\" " +
            $"u isto vrijeme{od}, pa kolicina ne moze biti manja od {vrhunac.Kolicina}.");
    }

    public async Task ZakljucajZaliheAsync(int poslovnicaId, CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            // Lock bez transakcije se otpusta cim se upit izvrsi, pa ne stiti nista.
            // To je greska u kodu koji poziva, ne nesto sto korisnik moze izazvati.
            throw new InvalidOperationException(
                "Zakljucavanje zaliha ima smisla samo unutar otvorene transakcije.");
        }

        // Zakljucavaju se svi redovi zaliha poslovnice, a ne samo trazene vrste.
        // Jedan upit sa jednim parametrom, a rezervacije opreme u istoj poslovnici
        // ionako rijetko stizu u istoj sekundi. Isti hint kao za vozilo: UPDLOCK
        // tjera drugog na cekanje, HOLDLOCK drzi lock do kraja transakcije.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT Id FROM StanjeOpreme WITH (UPDLOCK, HOLDLOCK) WHERE PoslovnicaId = {poslovnicaId}", ct);
    }

    /// <summary>
    /// Za svaku vrstu opreme: najvise komada koje rezervacije poslovnice drze u isto
    /// vrijeme unutar zadatog perioda. Baza vraca samo stavke rezervacija koje
    /// zauzimaju taj period; vrhunac se trazi nad tim, vec suzenim redovima.
    /// </summary>
    private async Task<Dictionary<int, Vrhunac>> ZauzetoAsync(
        int poslovnicaId, IReadOnlyCollection<int>? vrste,
        DateTime datumOd, DateTime datumDo, int? osimRezervacije, CancellationToken ct)
    {
        var rezervacije = _context.Rezervacije
            .Where(UslovDostupnosti.ZauzimaTermin(DateTime.UtcNow))
            .Where(r => r.PoslovnicaId == poslovnicaId
                        && r.DatumOd < datumDo
                        && r.DatumDo > datumOd);

        if (osimRezervacije is { } osim)
        {
            // Pri zamjeni vozila rezervacija ne smije smetati sama sebi.
            rezervacije = rezervacije.Where(r => r.Id != osim);
        }

        var stavke = rezervacije.SelectMany(
            r => r.StavkeOpreme,
            (r, s) => new { s.VrstaOpremeId, r.DatumOd, r.DatumDo, s.Kolicina });

        if (vrste is not null)
        {
            stavke = stavke.Where(s => vrste.Contains(s.VrstaOpremeId));
        }

        var zaduzenja = await stavke.ToListAsync(ct);

        return zaduzenja
            .GroupBy(x => x.VrstaOpremeId)
            .ToDictionary(
                g => g.Key,
                g => IstovremenaOprema.Najvise(g.Select(x => new Zaduzenje(x.DatumOd, x.DatumDo, x.Kolicina))));
    }
}
