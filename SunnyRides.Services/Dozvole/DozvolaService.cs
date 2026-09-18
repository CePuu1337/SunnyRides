using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Fajlovi;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Services.Dozvole;

public class DozvolaService
    : BaseService<VozackaDozvolaDto, VozackaDozvolaSearchObject, VozackaDozvola>, IDozvolaService
{
    private const string KljucPravila = "pravila-kategorija";
    private static readonly TimeSpan TrajanjeKesa = TimeSpan.FromMinutes(15);

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IMemoryCache _kes;
    private readonly IPohranaSlika _pohrana;
    private readonly IObjavljivacPoruka _objavljivac;

    public DozvolaService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IMemoryCache kes,
        IPohranaSlika pohrana,
        IObjavljivacPoruka objavljivac)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _kes = kes;
        _pohrana = pohrana;
        _objavljivac = objavljivac;
    }

    protected override string NazivEntiteta => "Vozacka dozvola";

    protected override string PodrazumijevaniPoredak => "DatumKreiranja";

    protected override IQueryable<VozackaDozvola> AddFilter(
        VozackaDozvolaSearchObject search, IQueryable<VozackaDozvola> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Klijent))
        {
            upit = upit.Where(x =>
                x.Korisnik.Ime.Contains(search.Klijent)
                || x.Korisnik.Prezime.Contains(search.Klijent)
                || x.Korisnik.Email.Contains(search.Klijent));
        }

        if (!string.IsNullOrWhiteSpace(search.BrojDozvole))
        {
            upit = upit.Where(x => x.BrojDozvole.Contains(search.BrojDozvole));
        }

        if (search.Status.HasValue)
        {
            upit = upit.Where(x => x.Status == search.Status.Value);
        }

        if (search.SamoIstekle == true)
        {
            var danas = DateTime.UtcNow;
            upit = upit.Where(x => x.DatumIsteka <= danas);
        }

        return upit;
    }

    protected override IQueryable<VozackaDozvola> AddInclude(
        VozackaDozvolaSearchObject search, IQueryable<VozackaDozvola> upit) => SaPovezanim(upit);

    protected override IQueryable<VozackaDozvola> AddIncludeDetalji(IQueryable<VozackaDozvola> upit) =>
        SaPovezanim(upit);

    private static IQueryable<VozackaDozvola> SaPovezanim(IQueryable<VozackaDozvola> upit) =>
        upit.Include(x => x.Korisnik)
            .Include(x => x.VerifikovaoKorisnik)
            .Include(x => x.Kategorije).ThenInclude(k => k.KategorijaDozvole);

    // --- klijentska strana -------------------------------------------------

    public async Task<VozackaDozvolaDto?> MojaAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var dozvola = await SaPovezanim(Context.VozackeDozvole)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KorisnikId == korisnikId, ct);

        return dozvola?.Adapt<VozackaDozvolaDto>();
    }

    public async Task<VozackaDozvolaDto> PrijaviAsync(
        VozackaDozvolaRequest request, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        ProvjeriDatume(request);
        await ProvjeriKategorijeAsync(request.KategorijaIds, ct);
        await ProvjeriBrojDozvoleAsync(request.BrojDozvole, korisnikId, ct);

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);

        var dozvola = await Context.VozackeDozvole
            .Include(x => x.Kategorije)
            .FirstOrDefaultAsync(x => x.KorisnikId == korisnikId, ct);

        if (dozvola is null)
        {
            dozvola = new VozackaDozvola
            {
                KorisnikId = korisnikId,
                DatumKreiranja = DateTime.UtcNow
            };
            Context.VozackeDozvole.Add(dozvola);
        }
        else
        {
            Context.DozvolaKategorije.RemoveRange(dozvola.Kategorije);
        }

        dozvola.BrojDozvole = request.BrojDozvole.Trim().ToUpperInvariant();
        dozvola.DatumIzdavanja = request.DatumIzdavanja;
        dozvola.DatumIsteka = request.DatumIsteka;

        // Svaka izmjena vraca dozvolu na cekanje i brise tragove ranije odluke.
        // Bez toga bi klijent mogao dobiti odobrenje za jednu dozvolu, pa joj poslije
        // promijeniti broj i kategorije, a odobrenje bi ostalo.
        dozvola.Status = StatusDozvole.NaCekanju;
        dozvola.RazlogOdbijanja = null;
        dozvola.VerifikovaoKorisnikId = null;
        dozvola.DatumVerifikacije = null;

        foreach (var kategorijaId in request.KategorijaIds.Distinct())
        {
            dozvola.Kategorije.Add(new DozvolaKategorija { KategorijaDozvoleId = kategorijaId });
        }

        await SacuvajAsync(ct);
        await transakcija.CommitAsync(ct);

        return await GetByIdAsync(dozvola.Id, ct);
    }

    public async Task<VozackaDozvolaDto> PostaviFotografijuAsync(
        Stream sadrzaj, long duzinaBajta, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var dozvola = await Context.VozackeDozvole
            .FirstOrDefaultAsync(x => x.KorisnikId == korisnikId, ct)
            ?? throw new BusinessException("Prvo prijavite dozvolu, pa onda dodajte fotografiju.");

        var stara = dozvola.PutanjaSlike;

        // Folder po korisniku, ne po dozvoli: korisnik ima najvise jednu dozvolu, a
        // ovako se pri brisanju naloga zna sta sve treba ukloniti.
        dozvola.PutanjaSlike = await _pohrana.SacuvajPrivatnoAsync(
            sadrzaj, duzinaBajta, $"dozvole/{korisnikId}", ct);

        // Nova fotografija znaci da uposlenik mora ponovo pogledati dozvolu. Bez ovoga
        // bi klijent odobrenu dozvolu mogao zamijeniti drugom slikom, a odobrenje bi
        // ostalo da vazi.
        dozvola.Status = StatusDozvole.NaCekanju;
        dozvola.RazlogOdbijanja = null;
        dozvola.VerifikovaoKorisnikId = null;
        dozvola.DatumVerifikacije = null;

        try
        {
            await Context.SaveChangesAsync(ct);
        }
        catch
        {
            // Upis je pao, a fajl je vec na disku - inace bi ostao bez ijednog zapisa
            // koji na njega pokazuje.
            _pohrana.ObrisiPrivatno(dozvola.PutanjaSlike);
            throw;
        }

        // Stara fotografija se brise tek kad je nova sigurno upisana.
        _pohrana.ObrisiPrivatno(stara);

        return await GetByIdAsync(dozvola.Id, ct);
    }

    public async Task<PrivatniFajl> PreuzmiFotografijuAsync(
        int dozvolaId, CancellationToken ct = default)
    {
        var dozvola = await Context.VozackeDozvole
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dozvolaId, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, dozvolaId);

        // Vlasnistvo se provjerava prema korisniku iz tokena, nikad prema vrijednosti
        // iz rute. Bez ove provjere bi svaki prijavljen korisnik mogao mijenjati broj
        // u adresi i preuzimati tudje vozacke dozvole.
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var jeOsoblje = _trenutniKorisnik.JeUUlozi(Uloge.Administrator)
                        || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

        if (dozvola.KorisnikId != korisnikId && !jeOsoblje)
        {
            throw new ForbiddenException("Mozete preuzeti samo fotografiju svoje dozvole.");
        }

        if (string.IsNullOrWhiteSpace(dozvola.PutanjaSlike))
        {
            throw new NotFoundException("Uz ovu dozvolu nije prilozena fotografija.");
        }

        return await _pohrana.OtvoriPrivatnoAsync(
            dozvola.PutanjaSlike, $"dozvola-{dozvola.BrojDozvole}.jpg", ct);
    }

    // --- uposlenicka strana ------------------------------------------------

    public async Task<VozackaDozvolaDto> OdobriAsync(int id, CancellationToken ct = default)
    {
        var dozvola = await NadjiAsync(id, ct);

        if (dozvola.Status == StatusDozvole.Odobrena)
        {
            throw new BusinessException("Dozvola je vec odobrena.");
        }

        if (dozvola.DatumIsteka <= DateTime.UtcNow)
        {
            throw new BusinessException(
                "Dozvola je istekla i ne moze se odobriti. Klijent mora prijaviti vazecu dozvolu.");
        }

        dozvola.Status = StatusDozvole.Odobrena;
        dozvola.RazlogOdbijanja = null;
        dozvola.VerifikovaoKorisnikId = _trenutniKorisnik.ObaveznoKorisnikId();
        dozvola.DatumVerifikacije = DateTime.UtcNow;

        await Context.SaveChangesAsync(ct);

        await _objavljivac.ObjaviAsync(Redovi.DozvolaVerifikovana, new DozvolaPoruka(id), ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<VozackaDozvolaDto> OdbijAsync(
        int id, OdbijDozvoluRequest request, CancellationToken ct = default)
    {
        var dozvola = await NadjiAsync(id, ct);

        if (dozvola.Status == StatusDozvole.Odbijena)
        {
            throw new BusinessException("Dozvola je vec odbijena.");
        }

        dozvola.Status = StatusDozvole.Odbijena;
        dozvola.RazlogOdbijanja = request.Razlog.Trim();
        dozvola.VerifikovaoKorisnikId = _trenutniKorisnik.ObaveznoKorisnikId();
        dozvola.DatumVerifikacije = DateTime.UtcNow;

        await Context.SaveChangesAsync(ct);

        // Ista poruka za odobrenje i odbijanje - worker iz baze vidi ishod i razlog.
        await _objavljivac.ObjaviAsync(Redovi.DozvolaVerifikovana, new DozvolaPoruka(id), ct);

        return await GetByIdAsync(id, ct);
    }

    // --- kategorije --------------------------------------------------------

    public async Task<DozvoljeneKategorijeDto> DozvoljeneKategorijeAsync(
        int korisnikId, DateTime? naDan = null, CancellationToken ct = default)
    {
        var datum = naDan ?? DateTime.UtcNow;

        var korisnik = await Context.Korisnici
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
            ?? throw NotFoundException.Za("Korisnik", korisnikId);

        var dozvola = await Context.VozackeDozvole
            .Include(x => x.Kategorije).ThenInclude(k => k.KategorijaDozvole)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KorisnikId == korisnikId, ct);

        var rezultat = new DozvoljeneKategorijeDto { KorisnikId = korisnikId };

        if (dozvola is null)
        {
            rezultat.Obrazlozenje = "Niste prijavili vozacku dozvolu, pa rezervacija jos nije moguca.";
            return rezultat;
        }

        rezultat.PosjedovaneKategorije = dozvola.Kategorije
            .Select(x => x.KategorijaDozvole.Oznaka)
            .OrderBy(x => x)
            .ToList();

        if (dozvola.Status != StatusDozvole.Odobrena)
        {
            rezultat.Obrazlozenje = dozvola.Status == StatusDozvole.Odbijena
                ? $"Dozvola je odbijena: {dozvola.RazlogOdbijanja}"
                : "Dozvola ceka verifikaciju. Do odobrenja mozete pretrazivati, ali ne i rezervisati.";
            return rezultat;
        }

        // Rok se provjerava na datum preuzimanja, a ne na danasnji dan. Dozvola koja
        // vazi danas a istice prije termina ne pokriva taj najam.
        if (dozvola.DatumIsteka <= datum)
        {
            rezultat.Obrazlozenje =
                $"Dozvola istice {dozvola.DatumIsteka:dd.MM.yyyy.}, prije trazenog termina.";
            return rezultat;
        }

        var godine = GodineNaDan(korisnik.DatumRodjenja, datum);
        await PopuniPokrivenostAsync(rezultat, dozvola, godine, ct);

        rezultat.MozeRezervisati = rezultat.DozvoljeneKategorijeIds.Count > 0;

        rezultat.Obrazlozenje = rezultat.DozvoljeneKategorijeIds.Count == 0
            ? $"Sa {godine} godina jos ne ispunjavate uslove nijedne kategorije sa svoje dozvole."
            : $"Prikazuju se samo vozila kategorija {string.Join(", ", rezultat.DozvoljeneKategorije)}, " +
              $"koja pokriva vasa dozvola ({string.Join(", ", rezultat.PosjedovaneKategorije)}).";

        return rezultat;
    }

    public async Task<DozvoljeneKategorijeDto> PokrivenostZaDozvoluAsync(
        int dozvolaId, CancellationToken ct = default)
    {
        var dozvola = await Context.VozackeDozvole
            .Include(x => x.Korisnik)
            .Include(x => x.Kategorije).ThenInclude(k => k.KategorijaDozvole)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dozvolaId, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, dozvolaId);

        var godine = GodineNaDan(dozvola.Korisnik.DatumRodjenja, DateTime.UtcNow);

        var rezultat = new DozvoljeneKategorijeDto
        {
            KorisnikId = dozvola.KorisnikId,
            PosjedovaneKategorije = dozvola.Kategorije
                .Select(x => x.KategorijaDozvole.Oznaka)
                .OrderBy(x => x)
                .ToList()
        };

        await PopuniPokrivenostAsync(rezultat, dozvola, godine, ct);

        // Ovdje se namjerno ne gleda status - uposlenik ovo cita dok odlucuje hoce li
        // dozvolu odobriti, pa mu treba odgovor na pitanje "sta bi smio voziti", a ne
        // podsjetnik da dozvola jos nije odobrena.
        rezultat.MozeRezervisati = false;

        var upozorenje = dozvola.DatumIsteka <= DateTime.UtcNow
            ? $" Napomena: dozvola je istekla {dozvola.DatumIsteka:dd.MM.yyyy.}"
            : string.Empty;

        rezultat.Obrazlozenje = rezultat.DozvoljeneKategorijeIds.Count == 0
            ? $"Klijent ima {godine} godina i ne ispunjava uslove nijedne kategorije sa ove dozvole.{upozorenje}"
            : $"Sa kategorijama {string.Join(", ", rezultat.PosjedovaneKategorije)} i {godine} godina, " +
              $"klijent bi smio voziti vozila kategorija " +
              $"{string.Join(", ", rezultat.DozvoljeneKategorije)}.{upozorenje}";

        return rezultat;
    }

    /// <summary>
    /// Jedno mjesto koje iz kategorija dozvole i godina klijenta izvodi pokrivene
    /// kategorije vozila. Zovu ga i klijentska i uposlenicka strana, pa ne mogu dati
    /// razlicit odgovor za istu dozvolu.
    /// </summary>
    private async Task PopuniPokrivenostAsync(
        DozvoljeneKategorijeDto rezultat, VozackaDozvola dozvola, int godine, CancellationToken ct)
    {
        var pravila = await PravilaAsync(ct);

        var posjedovane = dozvola.Kategorije.Select(x => x.KategorijaDozvoleId).ToList();
        var pokrivene = PravilaPokrivenosti.Pokrivene(pravila, posjedovane, godine);

        var oznake = await Context.KategorijeDozvola
            .Where(x => pokrivene.Contains(x.Id))
            .OrderBy(x => x.Oznaka)
            .Select(x => new { x.Id, x.Oznaka })
            .AsNoTracking()
            .ToListAsync(ct);

        rezultat.DozvoljeneKategorijeIds = oznake.Select(x => x.Id).ToList();
        rezultat.DozvoljeneKategorije = oznake.Select(x => x.Oznaka).ToList();
    }

    public async Task<List<int>> DozvoljeneKategorijeIdAsync(
        int korisnikId, DateTime? naDan = null, CancellationToken ct = default)
    {
        var rezultat = await DozvoljeneKategorijeAsync(korisnikId, naDan, ct);
        return rezultat.DozvoljeneKategorijeIds;
    }

    public async Task ObaveznoSmijeVozitiAsync(
        int korisnikId, int voziloId, DateTime datumPreuzimanja, CancellationToken ct = default)
    {
        var vozilo = await Context.Vozila
            .Include(x => x.ModelVozila).ThenInclude(m => m.KategorijaDozvole)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == voziloId, ct)
            ?? throw NotFoundException.Za("Vozilo", voziloId);

        var dozvoljene = await DozvoljeneKategorijeAsync(korisnikId, datumPreuzimanja, ct);

        if (dozvoljene.DozvoljeneKategorijeIds.Contains(vozilo.ModelVozila.KategorijaDozvoleId))
        {
            return;
        }

        // Kad klijent uopste nema upotrebljivu dozvolu, korisnija je poruka o tome
        // nego nabrajanje kategorija kojih nema.
        if (dozvoljene.DozvoljeneKategorije.Count == 0)
        {
            throw new BusinessException(dozvoljene.Obrazlozenje);
        }

        throw new BusinessException(
            $"Za ovo vozilo je potrebna kategorija {vozilo.ModelVozila.KategorijaDozvole.Oznaka}, " +
            $"a vasa dozvola pokriva {string.Join(" i ", dozvoljene.DozvoljeneKategorije)}.");
    }

    // --- interno -----------------------------------------------------------

    /// <summary>
    /// Pravila kategorija su sifrarnik od nekoliko redova koji se cita pri svakoj
    /// pretrazi, a mijenja kad se promijeni propis. Zato se kesiraju.
    ///
    /// Ovo nije "ucitaj sve pa filtriraj u memoriji" - to pravilo se odnosi na
    /// poslovne podatke. Ovdje se ucitava cijela tabela pravila zato sto se odluka o
    /// pokrivenosti donosi nad svim pravilima odjednom, a tabela ima pet redova.
    /// </summary>
    private async Task<List<PraviloUlaz>> PravilaAsync(CancellationToken ct)
    {
        if (_kes.TryGetValue(KljucPravila, out List<PraviloUlaz>? izKesa) && izKesa is not null)
        {
            return izKesa;
        }

        var pravila = await Context.PravilaKategorija
            .Select(x => new PraviloUlaz(
                x.KategorijaDozvoleId, x.TipVozilaId, x.MaxKubikaza, x.MaxSnagaKw, x.MinGodine))
            .AsNoTracking()
            .ToListAsync(ct);

        _kes.Set(KljucPravila, pravila, TrajanjeKesa);

        return pravila;
    }

    private async Task<VozackaDozvola> NadjiAsync(int id, CancellationToken ct) =>
        await Context.VozackeDozvole.FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw NotFoundException.Za(NazivEntiteta, id);

    private static void ProvjeriDatume(VozackaDozvolaRequest request)
    {
        if (request.DatumIsteka <= request.DatumIzdavanja)
        {
            throw new BusinessException("Datum isteka mora biti poslije datuma izdavanja.");
        }

        if (request.DatumIzdavanja > DateTime.UtcNow)
        {
            throw new BusinessException("Datum izdavanja ne moze biti u buducnosti.");
        }

        if (request.DatumIsteka <= DateTime.UtcNow)
        {
            throw new BusinessException("Dozvola je istekla i ne moze se prijaviti.");
        }
    }

    private async Task ProvjeriKategorijeAsync(List<int> kategorijaIds, CancellationToken ct)
    {
        var jedinstvene = kategorijaIds.Distinct().ToList();

        var postojece = await Context.KategorijeDozvola
            .Where(x => jedinstvene.Contains(x.Id))
            .CountAsync(ct);

        if (postojece != jedinstvene.Count)
        {
            throw new BusinessException("Jedna od odabranih kategorija ne postoji.");
        }
    }

    private async Task ProvjeriBrojDozvoleAsync(string broj, int korisnikId, CancellationToken ct)
    {
        var normalizovan = broj.Trim().ToUpperInvariant();

        var zauzet = await Context.VozackeDozvole
            .AnyAsync(x => x.BrojDozvole == normalizovan && x.KorisnikId != korisnikId, ct);

        if (zauzet)
        {
            throw new BusinessException("Dozvola sa tim brojem je vec prijavljena.");
        }
    }

    private static int GodineNaDan(DateTime datumRodjenja, DateTime naDan)
    {
        var godine = naDan.Year - datumRodjenja.Year;

        if (datumRodjenja.Date > naDan.Date.AddYears(-godine))
        {
            godine--;
        }

        return godine;
    }
}
