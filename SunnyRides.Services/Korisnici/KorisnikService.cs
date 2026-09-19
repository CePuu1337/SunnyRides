using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.Enums;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Korisnici;

public class KorisnikService
    : BaseCRUDService<KorisnikDto, KorisnikSearchObject, Korisnik,
                      KorisnikInsertRequest, KorisnikUpdateRequest>,
      IKorisnikService
{
    private const string Podfolder = "korisnici";

    /// <summary>Ista donja granica kao pri registraciji - nalog otvoren iz administracije nije izuzetak.</summary>
    private const int NajmanjeGodina = 16;

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IPohranaSlika _pohrana;

    public KorisnikService(
        SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik, IPohranaSlika pohrana)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _pohrana = pohrana;
    }

    protected override string NazivEntiteta => "Korisnik";

    protected override string PodrazumijevaniPoredak => "Prezime";

    protected override string PorukaZaDuplikat() =>
        "Korisnik sa tim korisnickim imenom ili email adresom vec postoji.";

    // --- odabir klijenta pri rucnom unosu ----------------------------------

    public async Task<PagedResult<KlijentZaOdabirDto>> KlijentiZaOdabirAsync(
        KlijentSearchObject search, CancellationToken ct = default)
    {
        // Samo aktivni nalozi sa ulogom klijenta. Nalozi osoblja se ovdje ne pojavljuju
        // ni kad se trazi po imenu - uposlenik preko ove liste ne moze doci do njih.
        var upit = Context.Korisnici
            .AsNoTracking()
            .Where(x => x.Aktivan && x.KorisnikRole.Any(kr => kr.Role.Naziv == Uloge.Klijent));

        if (!string.IsNullOrWhiteSpace(search.Tekst))
        {
            var tekst = search.Tekst.Trim();

            upit = upit.Where(x =>
                x.Ime.Contains(tekst)
                || x.Prezime.Contains(tekst)
                || x.Email.Contains(tekst)
                || (x.Telefon != null && x.Telefon.Contains(tekst)));
        }

        int? ukupno = search.IncludeTotalCount ? await upit.CountAsync(ct) : null;

        var stranica = Math.Max(search.Page ?? 0, 0);
        var velicina = Math.Clamp(
            search.PageSize ?? PodrazumijevanaVelicinaStranice, 1, MaksimalnaVelicinaStranice);

        var stavke = await upit
            .OrderBy(x => x.Prezime)
            .ThenBy(x => x.Ime)
            .ThenBy(x => x.Id)
            .Skip(stranica * velicina)
            .Take(velicina)
            .Select(x => new KlijentZaOdabirDto
            {
                Id = x.Id,
                Ime = x.Ime,
                Prezime = x.Prezime,
                Email = x.Email,
                Telefon = x.Telefon,
                Blokiran = x.Blokiran,
                StatusDozvole = x.VozackaDozvola == null
                    ? null
                    : (StatusDozvole?)x.VozackaDozvola.Status
            })
            .ToListAsync(ct);

        return new PagedResult<KlijentZaOdabirDto> { Items = stavke, TotalCount = ukupno };
    }

    // --- citanje -----------------------------------------------------------

    protected override IQueryable<Korisnik> AddFilter(
        KorisnikSearchObject search, IQueryable<Korisnik> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Tekst))
        {
            upit = upit.Where(x =>
                x.Ime.Contains(search.Tekst)
                || x.Prezime.Contains(search.Tekst)
                || x.KorisnickoIme.Contains(search.Tekst)
                || x.Email.Contains(search.Tekst));
        }

        if (!string.IsNullOrWhiteSpace(search.Uloga))
        {
            upit = upit.Where(x => x.KorisnikRole.Any(kr => kr.Role.Naziv == search.Uloga));
        }

        if (search.Aktivan.HasValue)
        {
            upit = upit.Where(x => x.Aktivan == search.Aktivan.Value);
        }

        if (search.Blokiran.HasValue)
        {
            upit = upit.Where(x => x.Blokiran == search.Blokiran.Value);
        }

        return upit;
    }

    protected override IQueryable<Korisnik> AddInclude(
        KorisnikSearchObject search, IQueryable<Korisnik> upit) => SaUlogama(upit);

    protected override IQueryable<Korisnik> AddIncludeDetalji(IQueryable<Korisnik> upit) =>
        SaUlogama(upit);

    private static IQueryable<Korisnik> SaUlogama(IQueryable<Korisnik> upit) =>
        upit.Include(x => x.KorisnikRole).ThenInclude(kr => kr.Role);

    public async Task<List<RoleDto>> UlogeAsync(CancellationToken ct = default)
    {
        return await Context.Role
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ProjectToType<RoleDto>()
            .ToListAsync(ct);
    }

    // --- upis --------------------------------------------------------------

    protected override async Task BeforeInsertAsync(
        KorisnikInsertRequest request, Korisnik entitet, CancellationToken ct)
    {
        await ProvjeriJedinstvenostAsync(request.KorisnickoIme, request.Email, null, ct);
        ProvjeriGodine(request.DatumRodjenja);

        entitet.LozinkaHash = BCrypt.Net.BCrypt.HashPassword(request.Lozinka);
        entitet.Aktivan = true;
        entitet.Blokiran = false;
        entitet.DatumRegistracije = DateTime.UtcNow;

        var uloge = await UcitajUlogeAsync(request.UlogeIds, ct);

        foreach (var uloga in uloge)
        {
            entitet.KorisnikRole.Add(new KorisnikRole
            {
                Role = uloga,
                DatumDodjele = DateTime.UtcNow
            });
        }
    }

    protected override async Task BeforeUpdateAsync(
        KorisnikUpdateRequest request, Korisnik entitet, CancellationToken ct)
    {
        await ProvjeriJedinstvenostAsync(null, request.Email, entitet.Id, ct);
        ProvjeriGodine(request.DatumRodjenja);

        // Administrator ne moze sam sebi deaktivirati nalog. Time bi se zakljucao van
        // sistema, a nalog koji bi to mogao ispraviti je upravo taj.
        if (!request.Aktivan && entitet.Id == _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new BusinessException("Ne mozete deaktivirati vlastiti nalog.");
        }
    }

    /// <summary>
    /// Korisnik se ne brise nego deaktivira.
    ///
    /// Njegove rezervacije, placanja i recenzije moraju ostati - bez njih izvjestaji o
    /// prihodu i iskoristenosti flote govore neistinu. Deaktiviran nalog se ne moze
    /// prijaviti, a historija ostaje citava.
    /// </summary>
    public override async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var korisnik = await Context.Korisnici.FirstOrDefaultAsync(x => x.Id == id, ct)
                       ?? throw NotFoundException.Za(NazivEntiteta, id);

        if (korisnik.Id == _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new BusinessException("Ne mozete deaktivirati vlastiti nalog.");
        }

        if (!korisnik.Aktivan)
        {
            return;
        }

        korisnik.Aktivan = false;
        await Context.SaveChangesAsync(ct);
    }

    // --- uloge -------------------------------------------------------------

    public async Task<KorisnikDto> PostaviUlogeAsync(
        int id, UlogeKorisnikaRequest request, CancellationToken ct = default)
    {
        var korisnik = await Context.Korisnici
            .Include(x => x.KorisnikRole).ThenInclude(kr => kr.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        var nove = await UcitajUlogeAsync(request.UlogeIds, ct);

        // Administrator ne moze ukloniti vlastitu administratorsku ulogu. Kad bi mogao,
        // ostao bi bez prava da tu gresku ispravi - a moguce je i da je on jedini
        // administrator u sistemu.
        var sebi = korisnik.Id == _trenutniKorisnik.ObaveznoKorisnikId();
        var imaoAdmina = korisnik.KorisnikRole.Any(kr => kr.Role.Naziv == Uloge.Administrator);
        var ostajeAdmin = nove.Any(x => x.Naziv == Uloge.Administrator);

        if (sebi && imaoAdmina && !ostajeAdmin)
        {
            throw new BusinessException("Ne mozete ukloniti vlastitu administratorsku ulogu.");
        }

        var postojece = korisnik.KorisnikRole.ToList();
        var noveIds = nove.Select(x => x.Id).ToHashSet();

        foreach (var veza in postojece.Where(x => !noveIds.Contains(x.RoleId)))
        {
            korisnik.KorisnikRole.Remove(veza);
            Context.Remove(veza);
        }

        foreach (var uloga in nove.Where(x => postojece.All(v => v.RoleId != x.Id)))
        {
            korisnik.KorisnikRole.Add(new KorisnikRole
            {
                Role = uloga,
                DatumDodjele = DateTime.UtcNow
            });
        }

        await Context.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    // --- lozinka -----------------------------------------------------------

    public async Task ResetujLozinkuAsync(
        int id, AdminResetLozinkeRequest request, CancellationToken ct = default)
    {
        var korisnik = await Context.Korisnici.FirstOrDefaultAsync(x => x.Id == id, ct)
                       ?? throw NotFoundException.Za(NazivEntiteta, id);

        // Stara lozinka se namjerno ne trazi - administrator je ne zna. Zato ovu radnju
        // smije pozvati iskljucivo administrator, sto stoji na kontroleru.
        korisnik.LozinkaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaLozinka);

        await Context.SaveChangesAsync(ct);
    }

    // --- blokada -----------------------------------------------------------

    public async Task<KorisnikDto> BlokirajAsync(int id, CancellationToken ct = default) =>
        await PostaviBlokaduAsync(id, true, ct);

    public async Task<KorisnikDto> OdblokirajAsync(int id, CancellationToken ct = default) =>
        await PostaviBlokaduAsync(id, false, ct);

    /// <summary>
    /// Blokada se odnosi na klijente. Blokiran klijent se i dalje moze prijaviti i
    /// vidjeti svoje rezervacije, ali ne moze napraviti novu - to pravilo provjerava
    /// <c>RezervacijaService</c> pri kreiranju.
    /// </summary>
    private async Task<KorisnikDto> PostaviBlokaduAsync(int id, bool blokiran, CancellationToken ct)
    {
        var korisnik = await Context.Korisnici
            .Include(x => x.KorisnikRole).ThenInclude(kr => kr.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        var jeOsoblje = korisnik.KorisnikRole.Any(kr =>
            kr.Role.Naziv == Uloge.Administrator || kr.Role.Naziv == Uloge.Uposlenik);

        if (jeOsoblje)
        {
            throw new BusinessException(
                "Blokada se odnosi na klijente. Nalog uposlenika se deaktivira, ne blokira.");
        }

        if (korisnik.Blokiran != blokiran)
        {
            korisnik.Blokiran = blokiran;
            await Context.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(id, ct);
    }

    // --- vlastiti profil ---------------------------------------------------

    public async Task<KorisnikDto> MojProfilAsync(CancellationToken ct = default) =>
        await GetByIdAsync(_trenutniKorisnik.ObaveznoKorisnikId(), ct);

    public async Task<KorisnikDto> AzurirajProfilAsync(
        ProfilUpdateRequest request, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var korisnik = await Context.Korisnici.FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
                       ?? throw NotFoundException.Za(NazivEntiteta, korisnikId);

        await ProvjeriJedinstvenostAsync(null, request.Email, korisnikId, ct);
        ProvjeriGodine(request.DatumRodjenja);

        // Mijenjaju se samo polja iz zahtjeva. Korisnicko ime, uloge, status naloga i
        // lozinka ostaju netaknuti - njih ovaj zahtjev ni ne nosi.
        korisnik.Ime = request.Ime.Trim();
        korisnik.Prezime = request.Prezime.Trim();
        korisnik.Email = request.Email.Trim();
        korisnik.Telefon = string.IsNullOrWhiteSpace(request.Telefon) ? null : request.Telefon.Trim();
        korisnik.DatumRodjenja = request.DatumRodjenja;

        await SacuvajAsync(ct);

        return await GetByIdAsync(korisnikId, ct);
    }

    public async Task<KorisnikDto> PostaviSlikuAsync(
        Stream sadrzaj, long duzinaBajta, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var korisnik = await Context.Korisnici.FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
                       ?? throw NotFoundException.Za(NazivEntiteta, korisnikId);

        var sacuvana = await _pohrana.SacuvajJavnoAsync(sadrzaj, duzinaBajta, Podfolder, ct);
        var stara = korisnik.PutanjaSlike;

        korisnik.PutanjaSlike = sacuvana.Putanja;
        await Context.SaveChangesAsync(ct);

        ObrisiSliku(stara);

        return await GetByIdAsync(korisnikId, ct);
    }

    public async Task<KorisnikDto> UkloniSlikuAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var korisnik = await Context.Korisnici.FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
                       ?? throw NotFoundException.Za(NazivEntiteta, korisnikId);

        if (korisnik.PutanjaSlike is null)
        {
            throw new BusinessException("Nemate postavljenu profilnu sliku.");
        }

        var stara = korisnik.PutanjaSlike;

        korisnik.PutanjaSlike = null;
        await Context.SaveChangesAsync(ct);

        ObrisiSliku(stara);

        return await GetByIdAsync(korisnikId, ct);
    }

    // --- pomocno -----------------------------------------------------------

    private async Task ProvjeriJedinstvenostAsync(
        string? korisnickoIme, string email, int? osimId, CancellationToken ct)
    {
        if (korisnickoIme is not null)
        {
            var zauzeto = await Context.Korisnici
                .AnyAsync(x => x.KorisnickoIme == korisnickoIme && (osimId == null || x.Id != osimId), ct);

            if (zauzeto)
            {
                throw new BusinessException($"Korisnicko ime \"{korisnickoIme}\" je vec zauzeto.");
            }
        }

        var emailZauzet = await Context.Korisnici
            .AnyAsync(x => x.Email == email && (osimId == null || x.Id != osimId), ct);

        if (emailZauzet)
        {
            throw new BusinessException("Nalog sa tom email adresom vec postoji.");
        }
    }

    private static void ProvjeriGodine(DateTime datumRodjenja)
    {
        var godine = DateTime.UtcNow.Year - datumRodjenja.Year;

        if (datumRodjenja.Date > DateTime.UtcNow.Date.AddYears(-godine))
        {
            godine--;
        }

        if (godine < NajmanjeGodina)
        {
            throw new BusinessException($"Korisnik mora imati najmanje {NajmanjeGodina} godina.");
        }
    }

    private async Task<List<Role>> UcitajUlogeAsync(List<int> ulogeIds, CancellationToken ct)
    {
        var trazene = ulogeIds.Distinct().ToList();

        if (trazene.Count == 0)
        {
            throw new BusinessException("Odaberite najmanje jednu ulogu.");
        }

        var uloge = await Context.Role.Where(x => trazene.Contains(x.Id)).ToListAsync(ct);

        if (uloge.Count != trazene.Count)
        {
            throw new BusinessException("Jedna od odabranih uloga ne postoji.");
        }

        return uloge;
    }

    private void ObrisiSliku(string? putanja)
    {
        if (putanja is null)
        {
            return;
        }

        _pohrana.ObrisiJavno(putanja, PutanjeSlika.Thumbnail(putanja));
    }
}
