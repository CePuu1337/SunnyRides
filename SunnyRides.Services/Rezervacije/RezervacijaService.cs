using Mapster;
using Microsoft.EntityFrameworkCore;
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
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Placanja;

namespace SunnyRides.Services.Rezervacije;

public class RezervacijaService
    : BaseService<RezervacijaDto, RezervacijaSearchObject, Rezervacija>, IRezervacijaService
{
    /// <summary>
    /// Koliko dugo vozilo ostaje rezervisano bez placanja. Poslije toga periodicni
    /// posao u workeru oslobadja termin, a provjera dostupnosti ga ne racuna kao
    /// zauzet ni prije nego worker stigne.
    /// </summary>
    private static readonly TimeSpan TrajanjeDrzanja = TimeSpan.FromMinutes(15);

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IDozvolaService _dozvolaService;
    private readonly IAvailabilityService _dostupnost;
    private readonly IPricingService _pricingService;
    private readonly IRezervacijaStateMachine _stateMachine;
    private readonly IIzvrsilacPovrata _izvrsilacPovrata;

    /// <summary>
    /// Kad zahtjev dolazi od klijenta, ovdje stoji njegov identifikator i lista se
    /// suzava na njegove rezervacije. Polje je u servisu, a ne u search objektu, jer
    /// se search objekat puni iz query stringa.
    /// </summary>
    private int? _ogranicenjeNaKorisnika;

    public RezervacijaService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IDozvolaService dozvolaService,
        IAvailabilityService dostupnost,
        IPricingService pricingService,
        IRezervacijaStateMachine stateMachine,
        IIzvrsilacPovrata izvrsilacPovrata)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _dozvolaService = dozvolaService;
        _dostupnost = dostupnost;
        _pricingService = pricingService;
        _stateMachine = stateMachine;
        _izvrsilacPovrata = izvrsilacPovrata;
    }

    protected override string NazivEntiteta => "Rezervacija";

    protected override string PodrazumijevaniPoredak => "DatumOd desc";

    // --- citanje -----------------------------------------------------------

    public override async Task<PagedResult<RezervacijaDto>> GetAsync(
        RezervacijaSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaKorisnika = JeOsoblje() ? null : _trenutniKorisnik.ObaveznoKorisnikId();

        return await base.GetAsync(search, ct);
    }

    public override async Task<RezervacijaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var rezervacija = await GetByIdOsnovnoAsync(id, ct);

        // Vlasnistvo se provjerava prema korisniku iz tokena. Bez ovoga bi svaki
        // prijavljen klijent mogao mijenjati broj u adresi i citati tudje rezervacije,
        // zajedno sa iznosima i kontakt podacima.
        if (!JeOsoblje() && rezervacija.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException("Mozete vidjeti samo svoje rezervacije.");
        }

        return rezervacija;
    }

    protected override IQueryable<Rezervacija> AddFilter(
        RezervacijaSearchObject search, IQueryable<Rezervacija> upit)
    {
        if (_ogranicenjeNaKorisnika.HasValue)
        {
            upit = upit.Where(x => x.KorisnikId == _ogranicenjeNaKorisnika.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Broj))
        {
            upit = upit.Where(x => x.Broj.Contains(search.Broj));
        }

        if (search.Status.HasValue)
        {
            upit = upit.Where(x => x.Status == search.Status.Value);
        }

        // Pretragu po klijentu koristi samo osoblje; klijentu je lista ionako vec
        // suzena na njegove zapise, pa bi ovaj uslov bio suvisan.
        if (!string.IsNullOrWhiteSpace(search.Klijent) && !_ogranicenjeNaKorisnika.HasValue)
        {
            upit = upit.Where(x =>
                x.Korisnik.Ime.Contains(search.Klijent)
                || x.Korisnik.Prezime.Contains(search.Klijent)
                || x.Korisnik.Email.Contains(search.Klijent));
        }

        if (!string.IsNullOrWhiteSpace(search.RegistarskaOznaka))
        {
            upit = upit.Where(x => x.Vozilo.RegistarskaOznaka.Contains(search.RegistarskaOznaka));
        }

        if (search.VoziloId.HasValue)
        {
            upit = upit.Where(x => x.VoziloId == search.VoziloId.Value);
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.PeriodOd.HasValue)
        {
            upit = upit.Where(x => x.DatumDo > search.PeriodOd.Value);
        }

        if (search.PeriodDo.HasValue)
        {
            upit = upit.Where(x => x.DatumOd < search.PeriodDo.Value);
        }

        if (search.IsPaid.HasValue)
        {
            upit = upit.Where(x => x.IsPaid == search.IsPaid.Value);
        }

        if (search.SamoAktivne.HasValue)
        {
            var sada = DateTime.UtcNow;

            // Aktivna je ona koja jos nije zavrsila i nije otkazana. Zavrsene i
            // otkazane idu u historiju, bez obzira na datume.
            upit = search.SamoAktivne.Value
                ? upit.Where(x => x.DatumDo > sada
                                  && x.Status != StatusRezervacije.Cancelled
                                  && x.Status != StatusRezervacije.Completed)
                : upit.Where(x => x.DatumDo <= sada
                                  || x.Status == StatusRezervacije.Cancelled
                                  || x.Status == StatusRezervacije.Completed);
        }

        return upit;
    }

    protected override IQueryable<Rezervacija> AddInclude(
        RezervacijaSearchObject search, IQueryable<Rezervacija> upit) => SaOsnovnim(upit);

    /// <summary>
    /// Detalj dodatno vuce stavke opreme i historiju statusa.
    ///
    /// Ide kroz <c>AsSplitQuery</c>, jer se ovdje ucitavaju tri kolekcije odjednom.
    /// U jednom upitu bi se redovi mnozili medjusobno - rezervacija sa tri stavke
    /// opreme i cetiri zapisa historije vratila bi dvanaest redova umjesto sedam, a
    /// svaki od njih ponovo sve podatke o vozilu i klijentu. EF na to i upozorava.
    /// Podjela je sigurna jer se dohvata jedan zapis po identifikatoru, bez paginacije.
    /// </summary>
    protected override IQueryable<Rezervacija> AddIncludeDetalji(IQueryable<Rezervacija> upit) =>
        SaOsnovnim(upit)
            .Include(x => x.StavkeOpreme).ThenInclude(s => s.VrstaOpreme)
            .Include(x => x.HistorijaStatusa.OrderBy(h => h.DatumVrijeme))
                .ThenInclude(h => h.IzvrsioKorisnik)
            .AsSplitQuery();

    /// <summary>
    /// Ono sto lista treba. Namjerno ima **tacno jednu** kolekciju - glavnu sliku
    /// vozila - pa se redovi ne mnoze i upit ostaje jedan.
    ///
    /// Stavke opreme i historija statusa se u listi ne prikazuju, pa se ni ne ucitavaju.
    /// Na DTO-u ostaju prazne liste; puni ih tek detaljni dohvat.
    /// </summary>
    private static IQueryable<Rezervacija> SaOsnovnim(IQueryable<Rezervacija> upit) =>
        upit.Include(x => x.Korisnik)
            .Include(x => x.OtkazaoKorisnik)
            .Include(x => x.RazlogOtkazivanja)
            .Include(x => x.Poslovnica)
            .Include(x => x.PaketOsiguranja)
            .Include(x => x.Vozilo).ThenInclude(v => v.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.Vozilo).ThenInclude(v => v.Slike.Where(s => s.JeGlavna));

    // --- kreiranje ---------------------------------------------------------

    public async Task<RezervacijaDto> KreirajAsync(
        RezervacijaInsertRequest request, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        await ProvjeriKlijentaAsync(korisnikId, ct);
        ProvjeriTermin(request.DatumOd, request.DatumDo);

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);

        // Redoslijed nije proizvoljan. Zakljucavanje ide prije provjere dostupnosti,
        // jer bi inace dva istovremena zahtjeva oba prosla provjeru prije nego ijedan
        // upise svoj red - provjera bi bila tacna u trenutku kad je radjena i pogresna
        // cim se drugi zahtjev upise.
        await _dostupnost.ZakljucajVoziloAsync(request.VoziloId, ct);

        var vozilo = await Context.Vozila
            .Include(x => x.ModelVozila)
            .FirstOrDefaultAsync(x => x.Id == request.VoziloId, ct)
            ?? throw NotFoundException.Za("Vozilo", request.VoziloId);

        if (!vozilo.Aktivno)
        {
            throw new BusinessException("Vozilo je povuceno iz ponude i ne moze se rezervisati.");
        }

        // Ista provjera koja je filtrirala pretragu radi se ponovo prije upisa, da se
        // ne moze zaobici pozivom API-ja direktno.
        await _dozvolaService.ObaveznoSmijeVozitiAsync(korisnikId, vozilo.Id, request.DatumOd, ct);

        await _dostupnost.ObaveznoSlobodnoAsync(vozilo.Id, request.DatumOd, request.DatumDo, ct: ct);

        await ProvjeriZaliheOpremeAsync(request.Oprema, vozilo.PoslovnicaId, ct);

        // Iznos racuna server. Klijent ga ne salje i ne bi mu se vjerovalo ni da ga posalje.
        var cijena = await _pricingService.IzracunajAsync(
            vozilo.Id, request.DatumOd, request.DatumDo, request.Oprema, request.PaketOsiguranjaId, ct);

        var rezervacija = new Rezervacija
        {
            // Privremena vrijednost: konacan broj se izvodi iz identifikatora, a njega
            // baza dodjeljuje tek pri upisu. Oba upisa su u istoj transakciji, pa ovu
            // vrijednost niko izvana ne vidi.
            Broj = $"TMP-{Guid.NewGuid():N}",

            KorisnikId = korisnikId,
            VoziloId = vozilo.Id,

            // Poslovnica se ne prima iz zahtjeva - vozilo se preuzima tamo gdje jeste.
            PoslovnicaId = vozilo.PoslovnicaId,

            PaketOsiguranjaId = cijena.PaketOsiguranjaId,
            DatumOd = request.DatumOd,
            DatumDo = request.DatumDo,
            Status = StatusRezervacije.Pending,
            UkupanIznos = cijena.UkupanIznos,
            IznosDepozita = cijena.IznosDepozita,
            IznosPopusta = cijena.IznosPopusta,
            IsPaid = false,
            DrziDo = DateTime.UtcNow.Add(TrajanjeDrzanja),
            DatumKreiranja = DateTime.UtcNow
        };

        foreach (var stavka in cijena.Oprema)
        {
            rezervacija.StavkeOpreme.Add(new StavkaOpreme
            {
                VrstaOpremeId = stavka.VrstaOpremeId,
                Kolicina = stavka.Kolicina,

                // Cijena se snima sada i vise se ne mijenja - kasnija izmjena
                // cjenovnika ne smije promijeniti historijsku rezervaciju.
                CijenaPoJedinici = stavka.CijenaPoJedinici,
                Iznos = stavka.Iznos
            });
        }

        _stateMachine.ZabiljeziKreiranje(rezervacija, "Rezervacija kreirana, ceka se placanje.");

        Context.Rezervacije.Add(rezervacija);
        await SacuvajAsync(ct);

        rezervacija.Broj = $"SR-{rezervacija.DatumOd:yyyy}-{rezervacija.Id:D5}";
        await SacuvajAsync(ct);

        await transakcija.CommitAsync(ct);

        return await GetByIdOsnovnoAsync(rezervacija.Id, ct);
    }

    // --- otkazivanje -------------------------------------------------------

    /// <summary>
    /// Prikaz posljedice, bez ijedne izmjene. Klijentska aplikacija ovo zove prije
    /// nego pokaze potvrdu, da korisnik vidi koliko gubi.
    /// </summary>
    public async Task<ObracunOtkazivanjaDto> ObracunOtkazivanjaAsync(
        int id, CancellationToken ct = default)
    {
        var rezervacija = await DohvatiZaOtkazivanjeAsync(id, ct);

        return NapraviObracun(rezervacija, DateTime.UtcNow);
    }

    /// <summary>
    /// Otkazivanje.
    ///
    /// Obracun se radi **iznova**, iz podataka u bazi i iz trenutnog vremena. Ono sto
    /// je klijent ranije vidio kroz <c>ObracunOtkazivanjaAsync</c> nije obecanje nego
    /// prikaz: da se taj odgovor uzimao zdravo za gotovo, klijent bi ga mogao dobiti
    /// osam dana prije termina, sacekati do dana prije, pa otkazati uz puni povrat.
    ///
    /// Redoslijed provjera: postoji - smijem li mu pristupiti - smije li se uopste
    /// otkazati - koliko se vraca - upis. Vlasnistvo ide odmah poslije postojanja, da
    /// tudji broj u adresi ne moze izvuci ni iznos ni razlog otkazivanja.
    /// </summary>
    public async Task<RezervacijaDto> OtkaziAsync(
        int id, OtkazivanjeRequest request, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();
        var otkazujeAgencija = JeOsoblje();

        var (razlog, napomena) = await ProvjeriRazlogAsync(request, otkazujeAgencija, ct);

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);

        // Otkazivanje i potvrda placanja iste rezervacije ne smiju raditi istovremeno
        // nad starim stanjem - onaj ko dodje drugi ceka ovdje.
        await Context.ZakljucajRezervacijuAsync(id, ct);

        var rezervacija = await DohvatiZaOtkazivanjeAsync(id, ct);

        var prepreka = RazlogNemogucnosti(rezervacija);
        if (prepreka is not null)
        {
            throw new BusinessException(prepreka);
        }

        var sada = DateTime.UtcNow;
        var obracun = NapraviObracun(rezervacija, sada);

        var tekstRazloga = napomena is null ? razlog.Naziv : $"{razlog.Naziv} - {napomena}";

        // Status mijenja iskljucivo state machine - ona provjerava prelaz i pise
        // audit zapis. Servis ga ne postavlja direktno ni ovdje.
        _stateMachine.Promijeni(
            rezervacija,
            StatusRezervacije.Cancelled,
            $"Rezervacija otkazana. {obracun.Obrazlozenje} " +
            $"Povrat: {obracun.UkupanPovrat:0.00} EUR.",
            tekstRazloga);

        rezervacija.RazlogOtkazivanjaId = razlog.Id;
        rezervacija.NapomenaOtkazivanja = napomena;
        rezervacija.DatumOtkazivanja = sada;

        // Ko je otkazao cita se iz tokena, nikad iz tijela zahtjeva.
        rezervacija.OtkazaoKorisnikId = korisnikId;

        // Drzanje vise nema smisla - termin je slobodan od ovog trenutka.
        rezervacija.DrziDo = null;

        EvidentirajPovrate(rezervacija, obracun, sada);

        await SacuvajAsync(ct);
        await transakcija.CommitAsync(ct);

        // Novac se salje tek kad je otkazivanje trajno upisano. Ako Stripe ne odgovori,
        // otkazivanje ostaje vazece, a povrat stoji zapisan i moze se poslati ponovo.
        // Otvoreni intenti se ponistavaju, da se otkazana rezervacija ne moze naplatiti.
        await _izvrsilacPovrata.IzvrsiZaRezervacijuAsync(rezervacija, ct);
        await SacuvajAsync(ct);

        return await GetByIdOsnovnoAsync(id, ct);
    }

    /// <summary>
    /// Razlog se bira iz liste, i klijent i agencija ga moraju izabrati. Agencija je
    /// tu posebno bitna jer klijent ima pravo znati zasto mu je najam otkazan, a taj
    /// razlog ide i u notifikaciju.
    ///
    /// Svaka strana smije izabrati samo razloge koji su predvidjeni za nju. Da nije
    /// tako, klijent bi preko API-ja mogao poslati "Vozilo je u kvaru" i izgledalo bi
    /// kao da je otkazala agencija.
    /// </summary>
    private async Task<(RazlogOtkazivanja Razlog, string? Napomena)> ProvjeriRazlogAsync(
        OtkazivanjeRequest request, bool otkazujeAgencija, CancellationToken ct)
    {
        var razlog = await Context.RazloziOtkazivanja
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RazlogOtkazivanjaId, ct)
            ?? throw new BusinessException("Odaberite razlog otkazivanja iz ponudjene liste.");

        if (!razlog.Aktivan)
        {
            throw new BusinessException(
                $"Razlog \"{razlog.Naziv}\" se vise ne nudi. Odaberite neki drugi.");
        }

        var dozvoljen = otkazujeAgencija ? razlog.ZaAgenciju : razlog.ZaKlijenta;
        if (!dozvoljen)
        {
            throw new BusinessException(otkazujeAgencija
                ? $"Razlog \"{razlog.Naziv}\" je predvidjen samo za klijente."
                : $"Razlog \"{razlog.Naziv}\" moze izabrati samo agencija.");
        }

        var napomena = string.IsNullOrWhiteSpace(request.Napomena) ? null : request.Napomena.Trim();

        if (razlog.TraziNapomenu && napomena is null)
        {
            throw new BusinessException(
                $"Uz razlog \"{razlog.Naziv}\" upisite kratko objasnjenje.");
        }

        return (razlog, napomena);
    }

    /// <summary>
    /// Dohvat sa svime sto obracun povrata treba: uspjesna placanja sa vec izvrsenim
    /// povratima, i primopredaje - zbog vozila koje je vec izdato.
    ///
    /// Ide kroz <c>AsSplitQuery</c> jer se ucitavaju dvije kolekcije, a jedna od njih
    /// ima svoju podkolekciju.
    /// </summary>
    private async Task<Rezervacija> DohvatiZaOtkazivanjeAsync(int id, CancellationToken ct)
    {
        var rezervacija = await Context.Rezervacije
            .Include(x => x.Placanja).ThenInclude(p => p.Refundi)
            .Include(x => x.Primopredaje)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        if (!JeOsoblje() && rezervacija.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException("Mozete otkazati samo svoje rezervacije.");
        }

        return rezervacija;
    }

    /// <summary>
    /// Zasto se ne moze otkazati, ili null ako moze. Isti izvor istine koristi i
    /// prikaz (polje <c>MozeSeOtkazati</c>) i sam upis, pa se ne moze desiti da dugme
    /// bude aktivno a zahtjev odbijen, ili obrnuto.
    /// </summary>
    private static string? RazlogNemogucnosti(Rezervacija rezervacija)
    {
        if (PrelaziRezervacije.JeTerminalan(rezervacija.Status))
        {
            return PrelaziRezervacije.PorukaOdbijanja(
                rezervacija.Status, StatusRezervacije.Cancelled);
        }

        // Vozilo koje je vec izdato se ne moze "otkazati" - ono se vraca. Taj put
        // vodi u Completed kroz primopredaju, ne u Cancelled.
        if (rezervacija.Primopredaje.Any(p => p.Tip == TipPrimopredaje.Izdavanje))
        {
            return "Vozilo je vec izdato, pa se rezervacija ne otkazuje nego zatvara " +
                   "evidentiranjem povrata vozila.";
        }

        return null;
    }

    /// <summary>
    /// Sastavlja obracun iz stvarno naplacenog iznosa.
    ///
    /// Gleda se <c>Placanje.NaplaceniIznos</c> uspjesnih placanja, a ne
    /// <c>Rezervacija.UkupanIznos</c> i nikako ne ponovni obracun iz cjenovnika. Ako
    /// je naplaceno manje nego sto rezervacija kaze, vraca se dio manjeg iznosa; ako
    /// se cjenovnik u medjuvremenu promijenio, povrat to ne osjeti.
    /// </summary>
    private ObracunOtkazivanjaDto NapraviObracun(Rezervacija rezervacija, DateTime sada)
    {
        var uspjesna = rezervacija.Placanja
            .Where(p => p.Status == StatusPlacanja.Succeeded)
            .ToList();

        var naplaceno = PravilaOtkazivanja.Zaokruzi(uspjesna.Sum(p => p.NaplaceniIznos ?? 0m));

        // Povrat koji je zapocet racuna se kao vec vracen. Da se gledaju samo izvrseni,
        // dva uzastopna poziva bi napravila dva povrata za isti novac.
        var vecVraceno = PravilaOtkazivanja.Zaokruzi(
            uspjesna.SelectMany(p => p.Refundi).Where(JeVazeci).Sum(r => r.Iznos));

        var obracun = PravilaOtkazivanja.Izracunaj(new UlazOtkazivanja
        {
            DatumOd = rezervacija.DatumOd,
            Sada = sada,
            OtkazujeAgencija = JeOsoblje(),
            Naplaceno = naplaceno,
            IznosDepozita = rezervacija.IznosDepozita,
            VecVraceno = vecVraceno
        });

        obracun.RezervacijaId = rezervacija.Id;
        obracun.Broj = rezervacija.Broj;
        obracun.Status = rezervacija.Status;

        var prepreka = RazlogNemogucnosti(rezervacija);
        obracun.MozeSeOtkazati = prepreka is null;
        obracun.RazlogNemogucnosti = prepreka;

        return obracun;
    }

    /// <summary>
    /// Upisuje povrat uz placanje kroz koje je novac i primljen. Stripe povrat vezuje
    /// za konkretan <c>PaymentIntent</c>, pa se iznos rasporedjuje po placanjima, a
    /// ne upisuje kao jedan slobodan zapis.
    ///
    /// Status je <c>Created</c> - zapis postoji, ali prema provajderu jos nije poslan.
    /// Salje ga <see cref="IIzvrsilacPovrata"/> poslije potvrde transakcije; tek kad
    /// Stripe odgovori, zapis postaje <c>Succeeded</c> ili <c>Failed</c>.
    /// </summary>
    private void EvidentirajPovrate(
        Rezervacija rezervacija, ObracunOtkazivanjaDto obracun, DateTime sada)
    {
        var ostatak = obracun.UkupanPovrat;

        if (ostatak <= 0)
        {
            return;
        }

        foreach (var placanje in rezervacija.Placanja
                     .Where(p => p.Status == StatusPlacanja.Succeeded)
                     .OrderBy(p => p.Id))
        {
            if (ostatak <= 0)
            {
                break;
            }

            var vecVraceno = placanje.Refundi.Where(JeVazeci).Sum(r => r.Iznos);
            var slobodno = PravilaOtkazivanja.Zaokruzi((placanje.NaplaceniIznos ?? 0m) - vecVraceno);

            if (slobodno <= 0)
            {
                continue;
            }

            var iznos = Math.Min(slobodno, ostatak);

            placanje.Refundi.Add(new Refund
            {
                Iznos = iznos,
                Razlog = obracun.Obrazlozenje,
                Status = StatusPlacanja.Created,
                KreiraoKorisnikId = _trenutniKorisnik.KorisnikId,
                DatumKreiranja = sada
            });

            ostatak = PravilaOtkazivanja.Zaokruzi(ostatak - iznos);
        }
    }

    private static bool JeVazeci(Refund refund) => IznosiStripe.PovratJeVazeci(refund.Status);

    // --- interno -----------------------------------------------------------

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

    /// <summary>Dohvat bez provjere vlasnistva - koristi se interno, poslije upisa.</summary>
    private async Task<RezervacijaDto> GetByIdOsnovnoAsync(int id, CancellationToken ct)
    {
        var rezervacija = await AddIncludeDetalji(Context.Rezervacije)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        return rezervacija.Adapt<RezervacijaDto>();
    }

    private async Task ProvjeriKlijentaAsync(int korisnikId, CancellationToken ct)
    {
        var klijent = await Context.Korisnici
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
            ?? throw NotFoundException.Za("Korisnik", korisnikId);

        // Blokiran klijent se moze prijaviti i vidjeti svoju historiju, ali ne moze
        // napraviti novu rezervaciju. To je smisao blokade - ne oduzima se pristup
        // vlastitim podacima nego pravo na novi najam.
        if (klijent.Blokiran)
        {
            throw new ForbiddenException(
                "Vas nalog je blokiran za nove rezervacije. Obratite se agenciji.");
        }

        if (!klijent.Aktivan)
        {
            throw new ForbiddenException("Nalog je deaktiviran.");
        }
    }

    private static void ProvjeriTermin(DateTime datumOd, DateTime datumDo)
    {
        if (datumDo <= datumOd)
        {
            throw new BusinessException("Datum vracanja mora biti poslije datuma preuzimanja.");
        }

        if (datumOd < DateTime.UtcNow)
        {
            throw new BusinessException("Termin preuzimanja je u proslosti.");
        }
    }

    /// <summary>
    /// Oprema koje nema na stanju u poslovnici preuzimanja se ne moze rezervisati.
    ///
    /// Gleda se stanje zaliha, ne i koliko je te opreme vec obecano drugim
    /// rezervacijama u istom terminu. Za obim ovog sistema - nekoliko komada po
    /// poslovnici i rijetki preklapajuci termini - to je prihvatljivo, a razlika je
    /// zabiljezena u dokumentaciji da se ne bi cinilo kao previd.
    /// </summary>
    private async Task ProvjeriZaliheOpremeAsync(
        List<StavkaOpremeRequest> oprema, int poslovnicaId, CancellationToken ct)
    {
        if (oprema.Count == 0)
        {
            return;
        }

        var idevi = oprema.Select(x => x.VrstaOpremeId).ToList();

        var stanja = await Context.StanjaOpreme
            .Where(x => x.PoslovnicaId == poslovnicaId && idevi.Contains(x.VrstaOpremeId))
            .Select(x => new { x.VrstaOpremeId, x.Kolicina, Naziv = x.VrstaOpreme.Naziv })
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var trazeno in oprema)
        {
            var stanje = stanja.FirstOrDefault(x => x.VrstaOpremeId == trazeno.VrstaOpremeId);

            if (stanje is null || stanje.Kolicina == 0)
            {
                throw new BusinessException(
                    "Odabrana oprema nije dostupna u poslovnici preuzimanja.");
            }

            if (stanje.Kolicina < trazeno.Kolicina)
            {
                throw new BusinessException(
                    $"Za opremu \"{stanje.Naziv}\" dostupno je {stanje.Kolicina} komada.");
            }
        }
    }
}
