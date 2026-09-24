using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Recenzije;

public class RecenzijaService
    : BaseCRUDService<RecenzijaDto, RecenzijaSearchObject, Recenzija,
                      RecenzijaInsertRequest, RecenzijaUpdateRequest>,
      IRecenzijaService
{
    private readonly ICurrentUserService _trenutniKorisnik;

    /// <summary>
    /// Kad zahtjev dolazi od klijenta, ovdje stoji njegov identifikator. Lista se tada
    /// suzava na neskrivene recenzije i njegove vlastite. Polje je u servisu, a ne u
    /// search objektu, jer se search objekat puni iz query stringa.
    /// </summary>
    private int? _ogranicenjeNaKorisnika;

    public RecenzijaService(SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
    }

    protected override string NazivEntiteta => "Recenzija";

    protected override string PodrazumijevaniPoredak => "DatumKreiranja desc";

    protected override string PorukaZaDuplikat() =>
        "Za ovu rezervaciju ste vec ostavili recenziju.";

    // --- citanje -----------------------------------------------------------

    public override async Task<PagedResult<RecenzijaDto>> GetAsync(
        RecenzijaSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaKorisnika = JeOsoblje() ? null : _trenutniKorisnik.ObaveznoKorisnikId();

        return await base.GetAsync(search, ct);
    }

    public override async Task<RecenzijaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var recenzija = await base.GetByIdAsync(id, ct);

        // Skrivenu recenziju vidi osoblje i njen autor. Autor mora moci vidjeti sta je
        // napisao i da je sklonjena; ostalima je nema.
        if (!JeOsoblje() && recenzija.Skrivena
            && recenzija.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw NotFoundException.Za(NazivEntiteta, id);
        }

        return recenzija;
    }

    protected override IQueryable<Recenzija> AddFilter(
        RecenzijaSearchObject search, IQueryable<Recenzija> upit)
    {
        if (_ogranicenjeNaKorisnika is { } korisnikId)
        {
            upit = search.SamoMoje
                ? upit.Where(x => x.KorisnikId == korisnikId)
                : upit.Where(x => !x.Skrivena || x.KorisnikId == korisnikId);
        }
        else if (search.SamoMoje)
        {
            // I uposlenik moze pogledati svoje recenzije, iako ih rijetko ima.
            var mojId = _trenutniKorisnik.ObaveznoKorisnikId();
            upit = upit.Where(x => x.KorisnikId == mojId);
        }

        if (search.VoziloId.HasValue)
        {
            upit = upit.Where(x => x.VoziloId == search.VoziloId.Value);
        }

        // Po modelu vozila, ne samo po primjerku: klijenta na detaljima vozila zanima
        // sta su ljudi rekli o tom modelu, a ne bas o toj registarskoj oznaci.
        if (search.ModelVozilaId.HasValue)
        {
            upit = upit.Where(x => x.Vozilo.ModelVozilaId == search.ModelVozilaId.Value);
        }

        if (search.RezervacijaId.HasValue)
        {
            upit = upit.Where(x => x.RezervacijaId == search.RezervacijaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Klijent) && _ogranicenjeNaKorisnika is null)
        {
            upit = upit.Where(x =>
                x.Korisnik.Ime.Contains(search.Klijent)
                || x.Korisnik.Prezime.Contains(search.Klijent)
                || x.Korisnik.Email.Contains(search.Klijent));
        }

        // Filter po skrivenosti je alat moderacije. Klijentu ne pomaze da trazi
        // skrivene - uslov iznad mu ionako ostavlja samo njegove.
        if (search.Skrivena.HasValue)
        {
            upit = upit.Where(x => x.Skrivena == search.Skrivena.Value);
        }

        if (search.OcjenaOd.HasValue)
        {
            upit = upit.Where(x => x.Ocjena >= search.OcjenaOd.Value);
        }

        if (search.OcjenaDo.HasValue)
        {
            upit = upit.Where(x => x.Ocjena <= search.OcjenaDo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Komentar))
        {
            upit = upit.Where(x => x.Komentar != null && x.Komentar.Contains(search.Komentar));
        }

        return upit;
    }

    protected override IQueryable<Recenzija> AddInclude(
        RecenzijaSearchObject search, IQueryable<Recenzija> upit) => SaPovezanim(upit);

    protected override IQueryable<Recenzija> AddIncludeDetalji(IQueryable<Recenzija> upit) =>
        SaPovezanim(upit);

    private static IQueryable<Recenzija> SaPovezanim(IQueryable<Recenzija> upit) =>
        upit.Include(x => x.Korisnik)
            .Include(x => x.Rezervacija)
            .Include(x => x.Vozilo).ThenInclude(v => v.ModelVozila).ThenInclude(m => m.Marka);

    // --- upis --------------------------------------------------------------

    /// <summary>
    /// Autor i vozilo se izvode iz rezervacije, ne iz zahtjeva.
    ///
    /// Provjerava se troje: rezervacija pripada onome ko pise, najam je zavrsen, i za
    /// tu rezervaciju jos nema recenzije. Prvo sprjecava potpisivanje tudjim imenom,
    /// drugo ocjenjivanje vozila koje klijent jos nije ni vratio, a trece visestruko
    /// ocjenjivanje istog najma - sto bi pomjerilo prosjecnu ocjenu vozila.
    /// </summary>
    protected override async Task BeforeInsertAsync(
        RecenzijaInsertRequest request, Recenzija entitet, CancellationToken ct)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var rezervacija = await Context.Rezervacije
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RezervacijaId, ct)
            ?? throw new BusinessException(
                $"Rezervacija sa identifikatorom {request.RezervacijaId} ne postoji.");

        if (rezervacija.KorisnikId != korisnikId)
        {
            throw new ForbiddenException("Mozete ocijeniti samo vlastiti najam.");
        }

        if (rezervacija.Status != StatusRezervacije.Completed)
        {
            throw new BusinessException(
                "Recenziju je moguce ostaviti tek kad je najam zavrsen i vozilo vraceno.");
        }

        var vecPostoji = await Context.Recenzije
            .AnyAsync(x => x.RezervacijaId == rezervacija.Id, ct);

        if (vecPostoji)
        {
            throw new BusinessException("Za ovu rezervaciju je recenzija vec ostavljena.");
        }

        entitet.KorisnikId = korisnikId;
        entitet.VoziloId = rezervacija.VoziloId;
        entitet.RezervacijaId = rezervacija.Id;
        entitet.Komentar = Ocisti(request.Komentar);
        entitet.DatumKreiranja = DateTime.UtcNow;
        entitet.Skrivena = false;
    }

    /// <summary>
    /// Izmjena je dozvoljena samo autoru i samo dok recenzija nije skrivena.
    ///
    /// Drugi uslov nije sitnica: bez njega bi klijent cija je recenzija sklonjena zbog
    /// neprimjerenog sadrzaja mogao promijeniti tekst i time je vratiti u opticaj, a da
    /// niko iz agencije to ne vidi.
    /// </summary>
    protected override Task BeforeUpdateAsync(
        RecenzijaUpdateRequest request, Recenzija entitet, CancellationToken ct)
    {
        if (entitet.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException("Mozete mijenjati samo vlastitu recenziju.");
        }

        if (entitet.Skrivena)
        {
            throw new BusinessException(
                "Recenzija je sklonjena zbog sadrzaja i vise se ne moze mijenjati. " +
                "Obratite se agenciji ako smatrate da je to greska.");
        }

        entitet.Komentar = Ocisti(request.Komentar);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Recenzija se ne brise nego skriva - tako prosjecna ocjena ostaje sljediva i
    /// vidi se da je nesto bilo pa sklonjeno. Zato brisanje ovdje znaci skrivanje.
    /// </summary>
    public override async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await SakrijAsync(id, ct);
    }

    // --- moderacija --------------------------------------------------------

    public async Task<RecenzijaDto> SakrijAsync(int id, CancellationToken ct = default) =>
        await PostaviSkrivenoAsync(id, true, ct);

    public async Task<RecenzijaDto> PrikaziAsync(int id, CancellationToken ct = default) =>
        await PostaviSkrivenoAsync(id, false, ct);

    private async Task<RecenzijaDto> PostaviSkrivenoAsync(int id, bool skrivena, CancellationToken ct)
    {
        if (!JeOsoblje())
        {
            throw new ForbiddenException("Moderaciju recenzija radi osoblje agencije.");
        }

        var recenzija = await Context.Recenzije.FirstOrDefaultAsync(x => x.Id == id, ct)
                        ?? throw NotFoundException.Za(NazivEntiteta, id);

        if (recenzija.Skrivena != skrivena)
        {
            recenzija.Skrivena = skrivena;
            await Context.SaveChangesAsync(ct);
        }

        return await base.GetByIdAsync(id, ct);
    }

    // --- sta jos ceka ocjenu -----------------------------------------------

    public async Task<List<RezervacijaZaRecenzijuDto>> ZaOcjenjivanjeAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        // Jedan upit sa provjerom nepostojanja recenzije, umjesto dohvata svih
        // zavrsenih rezervacija pa odbacivanja onih koje su vec ocijenjene.
        return await Context.Rezervacije
            .AsNoTracking()
            .Where(x => x.KorisnikId == korisnikId
                        && x.Status == StatusRezervacije.Completed
                        && !Context.Recenzije.Any(r => r.RezervacijaId == x.Id))
            .OrderByDescending(x => x.DatumDo)
            .ThenByDescending(x => x.Id)

            // Lista nema stranice jer je po prirodi kratka, ali gornja granica ipak
            // postoji - endpoint koji vraca "sve" bez limita uputstvo ne prihvata.
            .Take(MaksimalnaVelicinaStranice)
            .Select(x => new RezervacijaZaRecenzijuDto
            {
                RezervacijaId = x.Id,
                Broj = x.Broj,
                VoziloId = x.VoziloId,
                VoziloOpis = x.Vozilo.ModelVozila.Marka.Naziv + " " + x.Vozilo.ModelVozila.Naziv,
                ThumbnailUrl = x.Vozilo.Slike
                    .Where(s => s.JeGlavna)
                    .Select(s => s.PutanjaThumbnail)
                    .FirstOrDefault(),
                DatumOd = x.DatumOd,
                DatumDo = x.DatumDo
            })
            .ToListAsync(ct);
    }

    // --- pomocno -----------------------------------------------------------

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

    private static string? Ocisti(string? tekst) =>
        string.IsNullOrWhiteSpace(tekst) ? null : tekst.Trim();
}
