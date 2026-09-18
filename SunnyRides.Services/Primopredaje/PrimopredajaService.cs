using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Cijene;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Fajlovi;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Placanja;
using SunnyRides.Services.Poruke;
using SunnyRides.Services.Rezervacije;

namespace SunnyRides.Services.Primopredaje;

/// <summary>
/// Fizicko izdavanje i vracanje vozila.
///
/// Izdavanje ne mijenja status rezervacije - ona ostaje Confirmed, jer uputstvo
/// propisuje tacno cetiri statusa. Da je vozilo kod klijenta vidi se po tome sto
/// postoji zapis o izdavanju, a nema zapisa o povratu. Povrat zatvara rezervaciju
/// (Completed) i vraca ostatak depozita.
/// </summary>
public class PrimopredajaService
    : BaseService<PrimopredajaDto, PrimopredajaSearchObject, Primopredaja>, IPrimopredajaService
{
    /// <summary>Koliko najranije prije termina se vozilo moze izdati - koliko traje i priprema vozila.</summary>
    private static readonly TimeSpan RanoIzdavanje = TimeSpan.FromHours(2);

    private const int MaksimalnoFotografija = 10;

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IRezervacijaStateMachine _stateMachine;
    private readonly IPricingService _pricingService;
    private readonly IPohranaSlika _pohrana;
    private readonly IIzvrsilacPovrata _izvrsilacPovrata;
    private readonly IObjavljivacPoruka _objavljivac;
    private readonly ILogger<PrimopredajaService> _logger;

    private int? _ogranicenjeNaKorisnika;

    public PrimopredajaService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IRezervacijaStateMachine stateMachine,
        IPricingService pricingService,
        IPohranaSlika pohrana,
        IIzvrsilacPovrata izvrsilacPovrata,
        IObjavljivacPoruka objavljivac,
        ILogger<PrimopredajaService> logger)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _stateMachine = stateMachine;
        _pricingService = pricingService;
        _pohrana = pohrana;
        _izvrsilacPovrata = izvrsilacPovrata;
        _objavljivac = objavljivac;
        _logger = logger;
    }

    protected override string NazivEntiteta => "Primopredaja";

    protected override string PodrazumijevaniPoredak => "DatumVrijeme desc";

    // --- citanje -----------------------------------------------------------

    /// <summary>Osoblje vidi sve primopredaje, klijent samo one sa svojih rezervacija.</summary>
    public override async Task<PagedResult<PrimopredajaDto>> GetAsync(
        PrimopredajaSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaKorisnika = JeOsoblje() ? null : _trenutniKorisnik.ObaveznoKorisnikId();

        return await base.GetAsync(search, ct);
    }

    public override async Task<PrimopredajaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var vlasnikId = await Context.Primopredaje
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (int?)x.Rezervacija.KorisnikId)
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        ProvjeriVlasnistvo(vlasnikId, "Mozete vidjeti samo primopredaje svojih rezervacija.");

        return await base.GetByIdAsync(id, ct);
    }

    protected override IQueryable<Primopredaja> AddFilter(
        PrimopredajaSearchObject search, IQueryable<Primopredaja> upit)
    {
        if (_ogranicenjeNaKorisnika.HasValue)
        {
            upit = upit.Where(x => x.Rezervacija.KorisnikId == _ogranicenjeNaKorisnika.Value);
        }

        if (search.RezervacijaId.HasValue)
        {
            upit = upit.Where(x => x.RezervacijaId == search.RezervacijaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.RezervacijaBroj))
        {
            upit = upit.Where(x => x.Rezervacija.Broj.Contains(search.RezervacijaBroj));
        }

        if (search.Tip.HasValue)
        {
            upit = upit.Where(x => x.Tip == search.Tip.Value);
        }

        if (search.SaOstecenjem == true)
        {
            upit = upit.Where(x => x.EvidencijaStete != null);
        }

        if (search.DatumOd.HasValue)
        {
            upit = upit.Where(x => x.DatumVrijeme >= search.DatumOd.Value);
        }

        if (search.DatumDo.HasValue)
        {
            upit = upit.Where(x => x.DatumVrijeme < search.DatumDo.Value);
        }

        return upit;
    }

    protected override IQueryable<Primopredaja> AddInclude(
        PrimopredajaSearchObject search, IQueryable<Primopredaja> upit) => SaPovezanim(upit);

    protected override IQueryable<Primopredaja> AddIncludeDetalji(IQueryable<Primopredaja> upit) =>
        SaPovezanim(upit);

    private static IQueryable<Primopredaja> SaPovezanim(IQueryable<Primopredaja> upit) =>
        upit.Include(x => x.Rezervacija)
            .Include(x => x.IzvrsioKorisnik)
            .Include(x => x.EvidencijaStete)
            .Include(x => x.Fotografije);

    public async Task<PrivatniFajl> PreuzmiFotografijuAsync(int fotografijaId, CancellationToken ct = default)
    {
        var fotografija = await Context.FotografijePrimopredaje
            .AsNoTracking()
            .Where(x => x.Id == fotografijaId)
            .Select(x => new { x.Putanja, x.PrimopredajaId, x.Primopredaja.Rezervacija.KorisnikId })
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za("Fotografija", fotografijaId);

        ProvjeriVlasnistvo(fotografija.KorisnikId, "Mozete preuzeti samo fotografije svojih rezervacija.");

        return await _pohrana.OtvoriPrivatnoAsync(
            fotografija.Putanja, $"primopredaja-{fotografija.PrimopredajaId}-{fotografijaId}.jpg", ct);
    }

    // --- raspored ----------------------------------------------------------

    public async Task<PagedResult<RasporedStavkaDto>> RasporedAsync(
        RasporedSearchObject search, CancellationToken ct = default)
    {
        var od = search.Od ?? DateTime.UtcNow.Date;
        var @do = search.Do ?? od.AddDays(1);

        if (@do <= od)
        {
            throw new BusinessException("Kraj perioda mora biti poslije pocetka.");
        }

        if (@do - od > TimeSpan.FromDays(7))
        {
            throw new BusinessException("Raspored se prikazuje za najvise sedam dana odjednom.");
        }

        // U rasporedu su potvrdjene rezervacije (cekaju preuzimanje ili su kod klijenta)
        // i zavrsene (da se vidi i ono sto je danas vec vraceno).
        var upit = Context.Rezervacije
            .AsNoTracking()
            .Where(x => x.Status == StatusRezervacije.Confirmed || x.Status == StatusRezervacije.Completed)
            .Where(x => (x.DatumOd >= od && x.DatumOd < @do) || (x.DatumDo >= od && x.DatumDo < @do));

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        int? ukupno = search.IncludeTotalCount ? await upit.CountAsync(ct) : null;

        var velicina = Math.Clamp(search.PageSize ?? PodrazumijevanaVelicinaStranice, 1, MaksimalnaVelicinaStranice);
        var stranica = Math.Max(search.Page ?? 0, 0);

        // Jedan upit, sa projekcijom samo onoga sto red u rasporedu prikazuje.
        var redovi = await upit
            .OrderBy(x => x.DatumOd < od ? x.DatumDo : x.DatumOd)
            .Skip(stranica * velicina)
            .Take(velicina)
            .Select(x => new
            {
                x.Id,
                x.Broj,
                x.DatumOd,
                x.DatumDo,
                x.Status,
                Vozilo = x.Vozilo.ModelVozila.Marka.Naziv + " " + x.Vozilo.ModelVozila.Naziv,
                x.Vozilo.RegistarskaOznaka,
                Klijent = x.Korisnik.Ime + " " + x.Korisnik.Prezime,
                Poslovnica = x.Poslovnica.Naziv,
                Izdato = x.Primopredaje.Any(p => p.Tip == TipPrimopredaje.Izdavanje),
                Vraceno = x.Primopredaje.Any(p => p.Tip == TipPrimopredaje.Povrat)
            })
            .ToListAsync(ct);

        var stavke = new List<RasporedStavkaDto>();

        foreach (var r in redovi)
        {
            RasporedStavkaDto Stavka(TipPrimopredaje akcija, DateTime vrijeme, bool obavljeno) => new()
            {
                RezervacijaId = r.Id,
                Broj = r.Broj,
                Akcija = akcija,
                Vrijeme = vrijeme,
                VoziloNaziv = r.Vozilo,
                RegistarskaOznaka = r.RegistarskaOznaka,
                KlijentImePrezime = r.Klijent,
                PoslovnicaNaziv = r.Poslovnica,
                StatusRezervacije = r.Status,
                Obavljeno = obavljeno
            };

            if (r.DatumOd >= od && r.DatumOd < @do)
            {
                stavke.Add(Stavka(TipPrimopredaje.Izdavanje, r.DatumOd, r.Izdato));
            }

            if (r.DatumDo >= od && r.DatumDo < @do)
            {
                stavke.Add(Stavka(TipPrimopredaje.Povrat, r.DatumDo, r.Vraceno));
            }
        }

        return new PagedResult<RasporedStavkaDto>
        {
            Items = stavke.OrderBy(x => x.Vrijeme).ToList(),
            TotalCount = ukupno
        };
    }

    // --- izdavanje ---------------------------------------------------------

    public async Task<PrimopredajaDto> IzdajAsync(
        IzdavanjeVozilaRequest request, IReadOnlyList<UlazniFajl> fotografije, CancellationToken ct = default)
    {
        var uposlenikId = _trenutniKorisnik.ObaveznoKorisnikId();

        if (!request.KontrolnaListaProdjena)
        {
            throw new BusinessException("Prije izdavanja prodjite kontrolnu listu stanja vozila.");
        }

        ProvjeriBrojFotografija(fotografije);

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);
        await Context.ZakljucajRezervacijuAsync(request.RezervacijaId, ct);

        var rezervacija = await Context.Rezervacije
            .Include(x => x.Vozilo)
            .Include(x => x.Primopredaje)
            .FirstOrDefaultAsync(x => x.Id == request.RezervacijaId, ct)
            ?? throw NotFoundException.Za("Rezervacija", request.RezervacijaId);

        ProvjeriDaJePotvrdjena(rezervacija);

        if (rezervacija.Primopredaje.Any(x => x.Tip == TipPrimopredaje.Izdavanje))
        {
            throw new BusinessException("Vozilo je za ovu rezervaciju vec izdato.");
        }

        var sada = DateTime.UtcNow;

        if (sada < rezervacija.DatumOd - RanoIzdavanje)
        {
            throw new BusinessException(
                $"Vozilo se moze izdati najranije {RanoIzdavanje.TotalHours:0} sata prije termina " +
                $"({rezervacija.DatumOd:dd.MM.yyyy. HH:mm} UTC).");
        }

        if (sada >= rezervacija.DatumDo)
        {
            throw new BusinessException("Termin rezervacije je prosao, pa se vozilo vise ne moze izdati.");
        }

        if (request.Kilometraza < rezervacija.Vozilo.Kilometraza)
        {
            throw new BusinessException(
                $"Kilometraza ne moze biti manja od zadnje evidentirane ({rezervacija.Vozilo.Kilometraza} km).");
        }

        var izdavanje = new Primopredaja
        {
            RezervacijaId = rezervacija.Id,
            Tip = TipPrimopredaje.Izdavanje,
            DatumVrijeme = sada,
            Kilometraza = request.Kilometraza,
            NivoGoriva = request.NivoGoriva,
            KontrolnaListaProdjena = true,
            Napomena = Ocisti(request.Napomena),
            IzvrsioKorisnikId = uposlenikId
        };

        rezervacija.Vozilo.Kilometraza = request.Kilometraza;

        await SacuvajSaFotografijamaAsync(izdavanje, fotografije, ct);
        await transakcija.CommitAsync(ct);

        _logger.LogInformation(
            "Rezervacija {Broj}: vozilo izdato, {Km} km, gorivo {Gorivo} %.",
            rezervacija.Broj, izdavanje.Kilometraza, izdavanje.NivoGoriva);

        return await base.GetByIdAsync(izdavanje.Id, ct);
    }

    // --- povrat ------------------------------------------------------------

    public async Task<PrimopredajaDto> VratiAsync(
        PovratVozilaRequest request, IReadOnlyList<UlazniFajl> fotografije, CancellationToken ct = default)
    {
        var uposlenikId = _trenutniKorisnik.ObaveznoKorisnikId();
        var sada = DateTime.UtcNow;

        var (opisStete, iznosStete) = ProvjeriStetu(request, fotografije);
        ProvjeriBrojFotografija(fotografije);

        if (request.BlokirajVoziloDo.HasValue && request.BlokirajVoziloDo.Value <= sada)
        {
            throw new BusinessException("Datum do kojeg se vozilo blokira mora biti u buducnosti.");
        }

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);
        await Context.ZakljucajRezervacijuAsync(request.RezervacijaId, ct);

        var rezervacija = await UcitajZaPovratAsync(request.RezervacijaId, ct);

        ProvjeriDaJePotvrdjena(rezervacija);

        var izdavanje = rezervacija.Primopredaje.FirstOrDefault(x => x.Tip == TipPrimopredaje.Izdavanje)
            ?? throw new BusinessException("Vozilo za ovu rezervaciju nije ni izdato, pa se ne moze evidentirati povrat.");

        if (rezervacija.Primopredaje.Any(x => x.Tip == TipPrimopredaje.Povrat))
        {
            throw new BusinessException("Povrat vozila je za ovu rezervaciju vec evidentiran.");
        }

        if (request.Kilometraza < izdavanje.Kilometraza)
        {
            throw new BusinessException(
                $"Kilometraza pri povratu ne moze biti manja nego pri izdavanju ({izdavanje.Kilometraza} km).");
        }

        var obracun = await IzracunajAsync(rezervacija, sada, iznosStete, ct);

        var povrat = new Primopredaja
        {
            RezervacijaId = rezervacija.Id,
            Tip = TipPrimopredaje.Povrat,
            DatumVrijeme = sada,
            Kilometraza = request.Kilometraza,
            NivoGoriva = request.NivoGoriva,
            KontrolnaListaProdjena = true,
            Napomena = Ocisti(request.Napomena),
            IzvrsioKorisnikId = uposlenikId
        };

        if (opisStete is not null)
        {
            povrat.EvidencijaStete = new EvidencijaStete
            {
                Opis = opisStete,
                Iznos = iznosStete,
                DatumEvidentiranja = sada,
                EvidentiraoKorisnikId = uposlenikId
            };
        }

        rezervacija.Vozilo.Kilometraza = request.Kilometraza;

        // Status mijenja state machine, a u audit zapisu ostaje cijeli obracun.
        _stateMachine.Promijeni(rezervacija, StatusRezervacije.Completed,
            $"Evidentiran povrat vozila. {obracun.Obrazlozenje}");

        DodajPovratDepozita(rezervacija, obracun.PovratDepozita, obracun.Obrazlozenje, sada);

        if (request.BlokirajVoziloDo.HasValue)
        {
            var razlog = opisStete is null ? "Pregled vozila nakon povrata." : $"Ostecenje pri povratu: {opisStete}";

            Context.BlokadeVozila.Add(new BlokadaVozila
            {
                VoziloId = rezervacija.VoziloId,
                DatumOd = sada,
                DatumDo = request.BlokirajVoziloDo.Value,
                Razlog = razlog.Length > 500 ? razlog[..500] : razlog,
                KreiraoKorisnikId = uposlenikId,
                DatumKreiranja = sada
            });
        }

        await SacuvajSaFotografijamaAsync(povrat, fotografije, ct);
        await transakcija.CommitAsync(ct);

        // Depozit se vraca tek kad je povrat trajno upisan, isto kao kod otkazivanja.
        await _izvrsilacPovrata.IzvrsiZaRezervacijuAsync(rezervacija, ct);
        await SacuvajAsync(ct);

        _logger.LogInformation(
            "Rezervacija {Broj}: vozilo vraceno. {Obrazlozenje}", rezervacija.Broj, obracun.Obrazlozenje);

        // Poruka ide tek kad je sve upisano i kad je povrat depozita poslan, da klijent
        // u istoj poruci dobije i konacan obracun. Objava je van transakcije - poruka o
        // povratu koji nije prosao bila bi gora od poruke koja kasni.
        await _objavljivac.ObjaviAsync(
            Redovi.VoziloVraceno, new PrimopredajaPoruka(rezervacija.Id, povrat.Id), ct);

        return await base.GetByIdAsync(povrat.Id, ct);
    }

    public async Task<ObracunPovrataDto> ObracunPovrataAsync(
        int rezervacijaId, DateTime? datumPovrata, decimal? iznosStete, CancellationToken ct = default)
    {
        var rezervacija = await UcitajZaPovratAsync(rezervacijaId, ct, bezPracenja: true);

        return await IzracunajAsync(rezervacija, datumPovrata ?? DateTime.UtcNow, iznosStete ?? 0m, ct);
    }

    /// <summary>Jedan obracun za prikaz i za upis, da uposlenik vidi tacno ono sto ce se desiti.</summary>
    private async Task<ObracunPovrataDto> IzracunajAsync(
        Rezervacija rezervacija, DateTime datumPovrata, decimal iznosStete, CancellationToken ct)
    {
        var dnevnaCijena = await _pricingService.DnevnaCijenaAsync(rezervacija.VoziloId, rezervacija.DatumDo, ct);
        var depozit = UplaceniDepozit(rezervacija);

        var rezultat = ObracunPovrata.Izracunaj(new UlazPovrata(
            rezervacija.DatumDo, datumPovrata, dnevnaCijena, depozit, iznosStete));

        var izdavanje = rezervacija.Primopredaje.FirstOrDefault(x => x.Tip == TipPrimopredaje.Izdavanje);

        return new ObracunPovrataDto
        {
            RezervacijaId = rezervacija.Id,
            Broj = rezervacija.Broj,
            UgovorenoVracanje = rezervacija.DatumDo,
            DatumPovrata = datumPovrata,
            KasnjenjeMinuta = rezultat.KasnjenjeMinuta,
            UnutarTolerancije = rezultat.UnutarTolerancije,
            DanaPrekoracenja = rezultat.DanaPrekoracenja,
            DnevnaCijena = dnevnaCijena,
            Doplata = rezultat.Doplata,
            IznosStete = rezultat.IznosStete,
            UplaceniDepozit = depozit,
            ZadrzanoOdDepozita = rezultat.ZadrzanoOdDepozita,
            PovratDepozita = rezultat.PovratDepozita,
            NepokrivenoDepozitom = rezultat.NepokrivenoDepozitom,
            Obrazlozenje = rezultat.Obrazlozenje,
            KilometrazaPriIzdavanju = izdavanje?.Kilometraza,
            NivoGorivaPriIzdavanju = izdavanje?.NivoGoriva,
            DatumIzdavanja = izdavanje?.DatumVrijeme,
            IzdaoKorisnikIme = izdavanje?.IzvrsioKorisnik is null
                ? null
                : $"{izdavanje.IzvrsioKorisnik.Ime} {izdavanje.IzvrsioKorisnik.Prezime}",
            BrojFotografijaPriIzdavanju = izdavanje?.Fotografije.Count ?? 0
        };
    }

    /// <summary>
    /// Depozit koji je stvarno kod agencije: dio naplate koji je depozit, umanjen za
    /// ono sto je vec vraceno. Racuna se iz naplacenog iznosa, ne iz cjenovnika.
    /// </summary>
    private static decimal UplaceniDepozit(Rezervacija rezervacija)
    {
        var uspjesna = rezervacija.Placanja.Where(p => p.Status == StatusPlacanja.Succeeded).ToList();

        var naplaceno = uspjesna.Sum(p => p.NaplaceniIznos ?? 0m);
        var vraceno = uspjesna.SelectMany(p => p.Refundi)
            .Where(r => IznosiStripe.PovratJeVazeci(r.Status))
            .Sum(r => r.Iznos);

        return Math.Max(0m, Math.Min(rezervacija.IznosDepozita, naplaceno - vraceno));
    }

    private void DodajPovratDepozita(Rezervacija rezervacija, decimal iznos, string razlog, DateTime sada)
    {
        if (iznos <= 0)
        {
            return;
        }

        var placanje = rezervacija.Placanja
            .Where(p => p.Status == StatusPlacanja.Succeeded)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        if (placanje is null)
        {
            _logger.LogWarning(
                "Rezervacija {Broj}: depozit od {Iznos} EUR se ne moze vratiti jer nema uspjesnog placanja.",
                rezervacija.Broj, iznos);
            return;
        }

        placanje.Refundi.Add(new Refund
        {
            Iznos = iznos,
            Razlog = razlog.Length > 500 ? razlog[..500] : razlog,
            Status = StatusPlacanja.Created,
            KreiraoKorisnikId = _trenutniKorisnik.KorisnikId,
            DatumKreiranja = sada
        });
    }

    // --- pomocno -----------------------------------------------------------

    /// <summary>
    /// Kad je oznaceno ostecenje, opis, iznos i bar jedna fotografija su obavezni.
    /// Bez fotografije steta kasnije nema dokaza, a iznos se odbija od klijentovog depozita.
    /// </summary>
    private static (string? Opis, decimal Iznos) ProvjeriStetu(
        PovratVozilaRequest request, IReadOnlyList<UlazniFajl> fotografije)
    {
        var opis = Ocisti(request.OpisStete);

        if (!request.ImaOstecenje)
        {
            if (opis is not null || request.IznosStete is > 0)
            {
                throw new BusinessException(
                    "Opis i iznos stete unose se samo kad je oznaceno ostecenje.");
            }

            return (null, 0m);
        }

        if (opis is null || opis.Length < 5)
        {
            throw new BusinessException("Opisite ostecenje (najmanje 5 znakova).");
        }

        if (request.IznosStete is null or <= 0)
        {
            throw new BusinessException("Unesite iznos stete veci od nule.");
        }

        if (fotografije.Count == 0)
        {
            throw new BusinessException("Za evidentirano ostecenje prilozite najmanje jednu fotografiju.");
        }

        return (opis, request.IznosStete.Value);
    }

    private static void ProvjeriBrojFotografija(IReadOnlyList<UlazniFajl> fotografije)
    {
        if (fotografije.Count > MaksimalnoFotografija)
        {
            throw new BusinessException($"Uz primopredaju se moze priloziti najvise {MaksimalnoFotografija} fotografija.");
        }
    }

    private static void ProvjeriDaJePotvrdjena(Rezervacija rezervacija)
    {
        if (rezervacija.Status != StatusRezervacije.Confirmed)
        {
            throw new BusinessException(
                $"Primopredaja je moguca samo za potvrdjenu rezervaciju, a ova je {PrelaziRezervacije.Naziv(rezervacija.Status)}.");
        }

        if (!rezervacija.IsPaid)
        {
            throw new BusinessException("Rezervacija nije placena, pa se vozilo ne moze izdati ni vratiti.");
        }
    }

    /// <summary>
    /// Fotografije idu na disk prije upisa u bazu, jer se tek tada zna da su ispravne
    /// slike. Ako upis ne uspije, snimljeni fajlovi se brisu, da ne ostanu na disku
    /// bez ijednog zapisa koji na njih pokazuje.
    /// </summary>
    private async Task SacuvajSaFotografijamaAsync(
        Primopredaja primopredaja, IReadOnlyList<UlazniFajl> fotografije, CancellationToken ct)
    {
        var kljucevi = new List<string>();

        try
        {
            foreach (var fajl in fotografije)
            {
                var kljuc = await _pohrana.SacuvajPrivatnoAsync(
                    fajl.Sadrzaj, fajl.Duzina, $"primopredaje/{primopredaja.RezervacijaId}", ct);

                kljucevi.Add(kljuc);
                primopredaja.Fotografije.Add(new FotografijaPrimopredaje { Putanja = kljuc });
            }

            Context.Primopredaje.Add(primopredaja);
            await SacuvajAsync(ct);
        }
        catch
        {
            _pohrana.ObrisiPrivatno(kljucevi.ToArray());
            throw;
        }
    }

    private async Task<Rezervacija> UcitajZaPovratAsync(int rezervacijaId, CancellationToken ct, bool bezPracenja = false)
    {
        var upit = Context.Rezervacije
            .Include(x => x.Vozilo)
            .Include(x => x.Primopredaje).ThenInclude(p => p.IzvrsioKorisnik)
            .Include(x => x.Primopredaje).ThenInclude(p => p.Fotografije)
            .Include(x => x.Placanja).ThenInclude(p => p.Refundi)
            .AsSplitQuery();

        if (bezPracenja)
        {
            upit = upit.AsNoTracking();
        }

        return await upit.FirstOrDefaultAsync(x => x.Id == rezervacijaId, ct)
            ?? throw NotFoundException.Za("Rezervacija", rezervacijaId);
    }

    protected override string PorukaZaDuplikat() =>
        "Ova primopredaja je za rezervaciju vec evidentirana.";

    private void ProvjeriVlasnistvo(int vlasnikId, string poruka)
    {
        if (!JeOsoblje() && vlasnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException(poruka);
        }
    }

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

    private static string? Ocisti(string? tekst) =>
        string.IsNullOrWhiteSpace(tekst) ? null : tekst.Trim();
}
