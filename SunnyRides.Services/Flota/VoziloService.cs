using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
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
using SunnyRides.Services.Fajlovi;
using SunnyRides.Services.Lokacija;
using SunnyRides.Services.Preporuke;

namespace SunnyRides.Services.Flota;

public class VoziloService
    : BaseCRUDService<VoziloDto, VoziloSearchObject, Vozilo, VoziloInsertRequest, VoziloUpdateRequest>,
      IVoziloService
{
    private readonly IPohranaSlika _pohrana;
    private readonly IAvailabilityService _dostupnost;
    private readonly IDozvolaService _dozvolaService;
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IHistorijaPretrageService _historijaPretrage;
    private readonly IRecommenderService _recommender;
    private readonly IPricingService _pricing;
    private readonly IOpsegPoslovnice _opseg;
    private readonly ILokacijaKlijenta _lokacija;

    /// <summary>Poslovnica uposlenika; lista vozila se suzava na nju.</summary>
    private int? _ogranicenjeNaPoslovnicu;

    /// <summary>
    /// Gornja granica vozila koja ulaze u rangiranje po preporuci. Flota je desetak puta
    /// manja, pa granica nikad ne odsijeca stvarnu ponudu - stoji da bi upit ostao
    /// ogranicen i kad flota naraste.
    /// </summary>
    private const int MaksimalnoZaRangiranje = 500;

    /// <summary>
    /// Koliko rijeci teksta pretrage ulazi u upit. Marka i model zajedno rijetko imaju
    /// vise od tri; granica stoji da dug tekst ne napravi upit sa desetinama uslova.
    /// </summary>
    private const int NajviseRijeciPretrage = 5;

    /// <summary>
    /// Kategorije koje prijavljeni klijent smije voziti, razrijesene na pocetku
    /// pretrage.
    ///
    /// Drzi se u polju, a ne u search objektu, i to je sigurnosna odluka: search
    /// objekat se puni iz query stringa, pa bi lista kategorija u njemu bila nesto
    /// sto klijent moze sam poslati. Servis je Scoped, dakle traje jedan zahtjev.
    /// </summary>
    private List<int>? _dozvoljeneKategorije;

    /// <summary>
    /// True kad pretragu radi klijent. Tada server sam dodaje uslove koje klijent ne
    /// moze iskljuciti: samo aktivna vozila i samo kategorije koje pokriva dozvola.
    /// </summary>
    private bool _pretragaKlijenta;

    /// <summary>
    /// Dan za koji se racuna cijena u pretrazi: preuzimanje kad je termin zadan, inace
    /// danas. Isti dan koristi filter, poredak i cijena koja se vraca uz vozilo, pa se
    /// ne moze desiti da vozilo prodje filter "do 45 EUR" a prikaze 52 EUR.
    /// </summary>
    private DateTime _danCijene;

    public VoziloService(
        SunnyRidesDbContext context,
        IPohranaSlika pohrana,
        IAvailabilityService dostupnost,
        IDozvolaService dozvolaService,
        ICurrentUserService trenutniKorisnik,
        IHistorijaPretrageService historijaPretrage,
        IRecommenderService recommender,
        IPricingService pricing,
        IOpsegPoslovnice opseg,
        ILokacijaKlijenta lokacija)
        : base(context)
    {
        _pohrana = pohrana;
        _dostupnost = dostupnost;
        _dozvolaService = dozvolaService;
        _trenutniKorisnik = trenutniKorisnik;
        _historijaPretrage = historijaPretrage;
        _recommender = recommender;
        _pricing = pricing;
        _opseg = opseg;
        _lokacija = lokacija;
    }

    /// <summary>
    /// Kategorije se razrjesavaju ovdje jer je upit sinhron, a dozvola se cita iz baze.
    /// Filtriranje samo po sebi ostaje u <see cref="AddFilter"/>, kao i kod svih
    /// ostalih uslova.
    /// </summary>
    public override async Task<PagedResult<VoziloDto>> GetAsync(
        VoziloSearchObject search, CancellationToken ct = default)
    {
        _dozvoljeneKategorije = null;
        _ogranicenjeNaPoslovnicu = await _opseg.OgranicenjeAsync(ct);

        // Klijentu filter po dozvoli nije opcija koju ukljucuje aplikacija nego pravilo
        // servera. Da zavisi od parametra u upitu, direktan poziv API-ja bez njega vratio
        // bi i vozila koja klijent ne smije voziti. Uslov je "nije osoblje", a ne "jeste
        // klijent", da nalog bez prepoznate uloge dobije strozi, a ne blazi rezultat.
        _pretragaKlijenta = !JeOsoblje();
        _danCijene = ImaTermin(search) ? search.SlobodnoOd!.Value : DateTime.UtcNow;

        if (_pretragaKlijenta)
        {
            var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

            // Rok vazenja dozvole se provjerava na datum preuzimanja kad je poznat.
            _dozvoljeneKategorije = await _dozvolaService.DozvoljeneKategorijeIdAsync(
                korisnikId, search.SlobodnoOd, ct);
        }

        // Zapis o pretrazi je jedini trag o ukusu korisnika prije nego ista rezervise,
        // i bez njega sistem preporuke nema ulaz. Upisuje se ovdje, u jedinoj metodi
        // kroz koju pretraga stvarno prolazi - da se ne bi zaboravio na nekom od
        // mjesta koja je pozivaju.
        await _historijaPretrage.ZabiljeziAsync(search, ct);

        // Udaljenost se mjeri od lokacije telefona ili od grada iz profila. Za osoblje
        // nije poznato ni jedno, pa ostaje prazna i ne kosta upit.
        var polaziste = await _lokacija.PolazisteAsync(search.Latituda, search.Longituda, ct);
        var udaljenosti = polaziste is null
            ? null
            : await _lokacija.UdaljenostiPoslovnicaAsync(polaziste, ct);

        var rezultat = JePoredakPoPreporuci(search) || JePoredakPoUdaljenosti(search)
            ? await PoPreporuciAsync(search, JePoredakPoUdaljenosti(search) ? udaljenosti : null, ct)
            : await base.GetAsync(search, ct);

        await DodajOcjeneAsync(rezultat.Items, ct);
        await DodajCijeneZaPeriodAsync(rezultat.Items, search, ct);
        await DodajTarifeAsync(rezultat.Items, _danCijene, ct);
        DodajUdaljenosti(rezultat.Items, udaljenosti);

        return rezultat;
    }

    /// <summary>
    /// Cijena po danu i satu za vozila na stranici, kroz <see cref="IPricingService"/> -
    /// isti racun kao pri rezervaciji, sa tarifama sezone iz kesa.
    /// </summary>
    private async Task DodajTarifeAsync(
        IReadOnlyCollection<VoziloDto> vozila, DateTime dan, CancellationToken ct)
    {
        if (vozila.Count == 0)
        {
            return;
        }

        var tarife = await _pricing.TarifeNaDanAsync(vozila.Select(x => x.Id).ToList(), dan, ct);

        foreach (var vozilo in vozila)
        {
            if (tarife.TryGetValue(vozilo.Id, out var tarifa))
            {
                vozilo.Tarifa = tarifa;
            }
        }
    }

    /// <summary>
    /// Dnevna cijena koju klijent placa na dati dan, kao izraz koji EF prevodi u SQL.
    ///
    /// Pravilo je isto kao <see cref="PricingService.EfektivnaTarifa"/>: tarifa iz
    /// cjenovnika koji vazi za model tog dana, inace tarifa vozila, puta mnozilac,
    /// zaokruzeno na dvije decimale. Ponovljeno je ovdje jer filter i poredak moraju
    /// ici kroz <c>Where</c> i <c>OrderBy</c> na bazi (upute 8.2), a baza ne moze
    /// pozvati C# metodu. Preklapanje sezona je zabranjeno pri unosu, pa za dan postoji
    /// najvise jedan zapis cjenovnika.
    /// </summary>
    private Expression<Func<Vozilo, decimal>> DnevnaCijenaNaDan(DateTime dan) =>
        v => Math.Round(
            Context.Cjenovnici
                .Where(c => c.ModelVozilaId == v.ModelVozilaId && c.DatumOd <= dan && c.DatumDo > dan)
                .Select(c => (decimal?)((c.DnevnaTarifa ?? v.DnevnaTarifa) * c.Mnozilac))
                .FirstOrDefault() ?? v.DnevnaTarifa,
            2);

    /// <summary>"cijena >= granica" ili "cijena <= granica", nad izrazom cijene.</summary>
    private static Expression<Func<Vozilo, bool>> Uporedi(
        Expression<Func<Vozilo, decimal>> cijena, decimal granica, bool najmanje)
    {
        // Granica ide kroz zatvorenje, a ne kao konstanta, da je EF posalje kao
        // parametar upita umjesto da je upise u SQL.
        Expression<Func<decimal>> vrijednost = () => granica;

        var tijelo = najmanje
            ? Expression.GreaterThanOrEqual(cijena.Body, vrijednost.Body)
            : Expression.LessThanOrEqual(cijena.Body, vrijednost.Body);

        return Expression.Lambda<Func<Vozilo, bool>>(tijelo, cijena.Parameters);
    }

    private static void DodajUdaljenosti(
        IEnumerable<VoziloDto> vozila, IReadOnlyDictionary<int, double>? udaljenosti)
    {
        if (udaljenosti is null)
        {
            return;
        }

        foreach (var vozilo in vozila)
        {
            if (udaljenosti.TryGetValue(vozilo.PoslovnicaId, out var km))
            {
                vozilo.UdaljenostKm = Math.Round(km, 1);
            }
        }
    }

    /// <summary>
    /// Ukupna cijena za termin iz pretrage, samo za vozila na trazenoj stranici.
    ///
    /// Racuna je <see cref="IPricingService"/>, isti obracun koji racuna i rezervaciju,
    /// pa se iznos u rezultatima ne moze razlikovati od onoga sto rezervacija kasnije
    /// pokaze za isti period. Bez termina nema perioda, pa ostaje samo dnevna tarifa.
    /// </summary>
    private async Task DodajCijeneZaPeriodAsync(
        IReadOnlyCollection<VoziloDto> vozila, VoziloSearchObject search, CancellationToken ct)
    {
        if (vozila.Count == 0
            || search.SlobodnoOd is not { } od
            || search.SlobodnoDo is not { } doDatuma
            || doDatuma <= od)
        {
            return;
        }

        var cijene = await _pricing.IzracunajZaVozilaAsync(
            vozila.Select(x => x.Id).ToList(), od, doDatuma, ct);

        foreach (var vozilo in vozila)
        {
            if (!cijene.TryGetValue(vozilo.Id, out var cijena))
            {
                continue;
            }

            vozilo.CijenaZaPeriod = new CijenaPeriodaDto
            {
                IznosNajma = cijena.IznosNajma,
                NaplataPoSatu = cijena.NaplataPoSatu,
                BrojSati = cijena.BrojSati,
                BrojDana = cijena.BrojDana,
                ProcenatPopusta = cijena.ProcenatPopusta,
                IznosDepozita = cijena.IznosDepozita
            };
        }
    }

    /// <summary>
    /// Pretraga poredana po relevantnosti za prijavljenog korisnika, a kad su zadane
    /// udaljenosti - prvo po blizini poslovnice, pa po relevantnosti unutar nje.
    ///
    /// Svi uslovi pretrage - kategorije dozvole, dostupnost, grad, tip, cijena - idu
    /// kroz isti <see cref="AddFilter"/> i ostaju na bazi. Samo se poredak racuna u
    /// memoriji, jer ni skor ni udaljenost nisu kolone; zato se iz baze za rangiranje
    /// vuku samo identifikatori, a puni zapisi tek za trazenu stranicu.
    /// </summary>
    private async Task<PagedResult<VoziloDto>> PoPreporuciAsync(
        VoziloSearchObject search,
        IReadOnlyDictionary<int, double>? udaljenosti,
        CancellationToken ct)
    {
        var upit = AddFilter(search, Context.Vozila.AsQueryable());

        int? ukupno = search.IncludeTotalCount ? await upit.CountAsync(ct) : null;

        var redovi = await upit
            .OrderBy(x => x.Id)
            .Take(MaksimalnoZaRangiranje)
            .Select(x => new { x.Id, x.PoslovnicaId })
            .ToListAsync(ct);

        var rangirana = await _recommender.RangirajAsync(redovi.Select(x => x.Id).ToList(), ct);

        // Bez poznatog polazista "Najblize meni" nema od cega mjeriti, pa ostaje
        // obican poredak po preporuci.
        var poredak = udaljenosti is null
            ? PoredakPoPreporuci.Poredaj(rangirana)
            : PoredakPoUdaljenosti.Poredaj(
                rangirana, redovi.ToDictionary(x => x.Id, x => x.PoslovnicaId), udaljenosti);

        var stranica = Math.Max(search.Page ?? 0, 0);
        var velicina = Math.Clamp(
            search.PageSize ?? PodrazumijevanaVelicinaStranice, 1, MaksimalnaVelicinaStranice);

        var naStranici = poredak.Skip(stranica * velicina).Take(velicina).ToList();

        var vozila = await AddInclude(search, Context.Vozila.Where(x => naStranici.Contains(x.Id)))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, ct);

        // Baza ne zna za poredak po preporuci, pa se redoslijed vraca iz liste.
        return new PagedResult<VoziloDto>
        {
            Items = naStranici
                .Where(vozila.ContainsKey)
                .Select(id => vozila[id].Adapt<VoziloDto>())
                .ToList(),
            TotalCount = ukupno
        };
    }

    private static bool JePoredakPoPreporuci(VoziloSearchObject search) =>
        search.OrderBy is not null
        && search.OrderBy.StartsWith(PoredakRelevantnost, StringComparison.OrdinalIgnoreCase);

    private static bool JePoredakPoUdaljenosti(VoziloSearchObject search) =>
        search.OrderBy is not null
        && search.OrderBy.StartsWith(PoredakBlizina, StringComparison.OrdinalIgnoreCase);

    public override async Task<VoziloDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var vozilo = await base.GetByIdAsync(id, ct);

        await DodajOcjeneAsync(new[] { vozilo }, ct);

        return vozilo;
    }

    private Task DodajOcjeneAsync(IReadOnlyCollection<VoziloDto> vozila, CancellationToken ct) =>
        OcjeneVozila.DopuniAsync(Context, vozila, ct);

    /// <summary>
    /// Poredak po ocjeni ne moze kroz osnovnu implementaciju - ona sortira po
    /// kolonama entiteta, a prosjecna ocjena nije kolona nego racun nad recenzijama.
    /// Vozila bez ijedne recenzije zavrsavaju na kraju, bez obzira na smjer.
    /// </summary>
    protected override IQueryable<Vozilo> AddSort(VoziloSearchObject search, IQueryable<Vozilo> upit)
    {
        // Poredak po cijeni ide po cijeni koju klijent vidi i placa, ne po osnovnoj
        // tarifi vozila - inace bi "nize prvo" stavilo na vrh vozilo koje je u sezoni
        // skuplje od onog ispod njega.
        if (search.OrderBy is not null
            && search.OrderBy.StartsWith(PoredakPoCijeni, StringComparison.OrdinalIgnoreCase))
        {
            var cijena = DnevnaCijenaNaDan(_danCijene);
            var opadajuce = search.OrderBy.Contains("desc", StringComparison.OrdinalIgnoreCase);

            var poCijeni = opadajuce ? upit.OrderByDescending(cijena) : upit.OrderBy(cijena);

            return poCijeni.ThenBy(x => x.Id);
        }

        if (search.OrderBy is null
            || !search.OrderBy.StartsWith(PoredakPoOcjeni, StringComparison.OrdinalIgnoreCase))
        {
            return base.AddSort(search, upit);
        }

        var rastuce = search.OrderBy.Contains("asc", StringComparison.OrdinalIgnoreCase);

        // Poredak se pise kao izraz nad samim upitom, bez medjuprojekcije: projekcija
        // bi odbacila Include-ove postavljene prije sortiranja, pa bi vozilo ostalo
        // bez marke, tipa i poslovnice. Zamjenska vrijednost za neocijenjena vozila
        // je van skale u oba smjera, da uvijek zavrse na kraju.
        var sortirano = rastuce
            ? upit.OrderBy(x => x.Recenzije.Where(r => !r.Skrivena).Average(r => (double?)r.Ocjena) ?? 6d)
            : upit.OrderByDescending(x => x.Recenzije.Where(r => !r.Skrivena).Average(r => (double?)r.Ocjena) ?? -1d);

        // Vozila iste ocjene (i sva neocijenjena) inace nemaju stalan redoslijed, pa bi
        // beskonacni skrol u mobilnoj aplikaciji mogao dvaput prikazati isto vozilo.
        return sortirano.ThenBy(x => x.Id);
    }

    /// <summary>Vrijednost koju mobilna aplikacija salje kad korisnik sortira po ocjeni.</summary>
    public const string PoredakPoOcjeni = "ProsjecnaOcjena";

    /// <summary>
    /// Poredak po dnevnoj cijeni za dan preuzimanja ("Cijena asc" / "Cijena desc").
    /// Nije ime kolone: cijena se racuna iz tarife i sezone.
    /// </summary>
    public const string PoredakPoCijeni = "Cijena";

    /// <summary>
    /// Vrijednost za poredak po relevantnosti - "Preporuceno za vas" u mobilnoj
    /// aplikaciji.
    /// </summary>
    public const string PoredakRelevantnost = "Preporuka";

    /// <summary>
    /// Vrijednost za poredak "Najblize meni" - podrazumijevani poredak pretrage u
    /// mobilnoj aplikaciji. Poslovnice idu od najblize, a vozila unutar iste
    /// poslovnice po preporuci.
    /// </summary>
    public const string PoredakBlizina = "Udaljenost";

    protected override string NazivEntiteta => "Vozilo";

    protected override string PodrazumijevaniPoredak => "RegistarskaOznaka";

    protected override IQueryable<Vozilo> AddFilter(VoziloSearchObject search, IQueryable<Vozilo> upit)
    {
        // Uposlenik u administraciji flote vidi samo vozila svoje poslovnice.
        if (_ogranicenjeNaPoslovnicu is { } poslovnicaUposlenika)
        {
            upit = upit.Where(x => x.PoslovnicaId == poslovnicaUposlenika);
        }

        if (!string.IsNullOrWhiteSpace(search.ModelNaziv))
        {
            // Svaka rijec mora postojati u marki ili u modelu, pa "Honda", "PCX" i
            // "Honda PCX" nadju isto vozilo. Svaka rijec je jedan LIKE uslov na bazi.
            var rijeci = search.ModelNaziv
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(NajviseRijeciPretrage);

            foreach (var rijec in rijeci)
            {
                upit = upit.Where(x => x.ModelVozila.Naziv.Contains(rijec)
                                       || x.ModelVozila.Marka.Naziv.Contains(rijec));
            }
        }

        if (!string.IsNullOrWhiteSpace(search.RegistarskaOznaka))
        {
            upit = upit.Where(x => x.RegistarskaOznaka.Contains(search.RegistarskaOznaka));
        }

        if (search.ModelVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozilaId == search.ModelVozilaId.Value);
        }

        if (search.MarkaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.MarkaId == search.MarkaId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipVozilaId == search.TipVozilaId.Value);
        }

        if (search.TipGorivaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipGorivaId == search.TipGorivaId.Value);
        }

        // Filter osoblja po potrebnoj kategoriji. Klijenta ogranicava njegova dozvola,
        // nize, bez obzira na ovaj parametar.
        if (search.KategorijaDozvoleId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.KategorijaDozvoleId == search.KategorijaDozvoleId.Value);
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.GradId.HasValue)
        {
            upit = upit.Where(x => x.Poslovnica.GradId == search.GradId.Value);
        }

        // Raspon cijene se poredi sa cijenom koju klijent vidi za dan preuzimanja, a ne
        // sa osnovnom tarifom vozila. Vozilo od 40 EUR u sezoni sa mnoziocem 1,3 kosta
        // 52 EUR i ne smije proci filter "do 45 EUR".
        if (search.CijenaOd.HasValue)
        {
            upit = upit.Where(Uporedi(DnevnaCijenaNaDan(_danCijene), search.CijenaOd.Value, najmanje: true));
        }

        if (search.CijenaDo.HasValue)
        {
            upit = upit.Where(Uporedi(DnevnaCijenaNaDan(_danCijene), search.CijenaDo.Value, najmanje: false));
        }

        if (search.Aktivno.HasValue)
        {
            upit = upit.Where(x => x.Aktivno == search.Aktivno.Value);
        }

        if (_pretragaKlijenta)
        {
            // Deaktivirano vozilo je povuceno iz ponude i klijent ga ne vidi, sta god
            // poslao u parametru Aktivno.
            upit = upit.Where(x => x.Aktivno);

            // Filtriranje po dozvoli ide na serveru, ne skrivanjem stavki u interfejsu.
            // Prazna lista znaci da klijent nema upotrebljivu dozvolu - tada se ne
            // prikazuje nijedno vozilo, jer nijedno ne smije voziti.
            var kategorije = _dozvoljeneKategorije ?? new List<int>();
            upit = upit.Where(x => kategorije.Contains(x.ModelVozila.KategorijaDozvoleId));

            // Bez termina se dostupnost ne moze provjeriti, ali vozilo koje je upravo
            // na servisu ne smije izgledati kao da je u ponudi.
            if (!ImaTermin(search))
            {
                upit = _dostupnost.DodajUslovBezAktivneBlokade(upit, DateTime.UtcNow);
            }
        }

        // Uslov slobodnog termina se ne pise ovdje nego se trazi od servisa koji ga
        // jedini poznaje. Vraca se kao dio istog upita, pa baza odbacuje zauzeta
        // vozila prije nego ijedan red stigne u memoriju.
        if (ImaTermin(search))
        {
            upit = _dostupnost.DodajUslovSlobodno(upit, search.SlobodnoOd!.Value, search.SlobodnoDo!.Value);
        }

        return upit;
    }

    /// <summary>Termin vazi samo kad su zadana oba datuma, i to pravim redoslijedom.</summary>
    private static bool ImaTermin(VoziloSearchObject search) =>
        search.SlobodnoOd.HasValue && search.SlobodnoDo.HasValue
        && search.SlobodnoDo.Value > search.SlobodnoOd.Value;

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

    // Lista vuce samo glavnu sliku, i to filtriranim Include-om - ostale fotografije
    // ostaju na serveru dok ih neko stvarno ne zatrazi kroz /api/vozila/{id}/slike.
    protected override IQueryable<Vozilo> AddInclude(VoziloSearchObject search, IQueryable<Vozilo> upit) =>
        upit.SaPovezanim();

    protected override IQueryable<Vozilo> AddIncludeDetalji(IQueryable<Vozilo> upit) =>
        upit.SaPovezanim();

    protected override string PorukaZaDuplikat() =>
        "Vozilo sa tom registarskom oznakom vec postoji.";

    protected override async Task BeforeInsertAsync(
        VoziloInsertRequest request, Vozilo entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.ModelVozilaId, request.PoslovnicaId, ct);
        await _opseg.ObaveznoDozvoljenaAsync(request.PoslovnicaId, ct);
        ProvjeriGodinu(request.GodinaProizvodnje);
        ProvjeriTarife(request.SatnaTarifa, request.DnevnaTarifa);

        entitet.RegistarskaOznaka = NormalizujOznaku(request.RegistarskaOznaka);
        entitet.Aktivno = true;
        entitet.DatumKreiranja = DateTime.UtcNow;
    }

    protected override async Task BeforeUpdateAsync(
        VoziloUpdateRequest request, Vozilo entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.ModelVozilaId, request.PoslovnicaId, ct);

        // Uposlenik mijenja samo vozila svoje poslovnice i ne moze ih premjestiti u
        // drugu. Stara poslovnica se cita prije nego je zahtjev prepisao.
        var dosadasnja = Context.Entry(entitet).Property(x => x.PoslovnicaId).OriginalValue;
        await _opseg.ObaveznoDozvoljenaAsync(dosadasnja, ct);
        await _opseg.ObaveznoDozvoljenaAsync(request.PoslovnicaId, ct);

        ProvjeriGodinu(request.GodinaProizvodnje);
        ProvjeriTarife(request.SatnaTarifa, request.DnevnaTarifa);

        entitet.RegistarskaOznaka = NormalizujOznaku(request.RegistarskaOznaka);
    }

    /// <summary>
    /// Vozilo koje ima rezervacije se ne brise. Historija najmova mora ostati citava
    /// radi izvjestaja, a brisanje vozila povuklo bi za sobom dio te historije ili bi
    /// puklo na stranom kljucu. Umjesto toga se deaktivira - nestane iz pretrage i
    /// kalendara flote, a proslost ostaje netaknuta.
    /// </summary>
    protected override async Task BeforeDeleteAsync(Vozilo entitet, CancellationToken ct)
    {
        var brojRezervacija = await Context.Rezervacije.CountAsync(x => x.VoziloId == entitet.Id, ct);
        if (brojRezervacija > 0)
        {
            throw new BusinessException(
                $"Vozilo \"{entitet.RegistarskaOznaka}\" se ne moze obrisati jer postoje rezervacije " +
                $"({brojRezervacija}). Deaktivirajte ga umjesto brisanja.");
        }

        var brojBlokada = await Context.BlokadeVozila.CountAsync(x => x.VoziloId == entitet.Id, ct);
        if (brojBlokada > 0)
        {
            throw new BusinessException(
                $"Vozilo \"{entitet.RegistarskaOznaka}\" se ne moze obrisati jer za njega postoje " +
                $"evidentirane blokade ({brojBlokada}).");
        }

        // Slike se u bazi brisu kaskadno, ali fajlovi na disku ne - njih uklanjamo
        // ovdje, dok se jos zna gdje su.
        // Iz baze se povlace samo dvije kolone, pa se par putanja spaja u listu
        // ovdje. SelectMany sa novim nizom SQL ne zna prevesti - ovo bi puklo u
        // trenutku brisanja, a ne pri prevodjenju.
        var slike = await Context.SlikeVozila
            .Where(x => x.VoziloId == entitet.Id)
            .Select(x => new { x.Putanja, x.PutanjaThumbnail })
            .ToListAsync(ct);

        _pohrana.ObrisiJavno(
            slike.SelectMany(x => new[] { x.Putanja, x.PutanjaThumbnail }).ToArray());
    }

    private async Task ProvjeriVezeAsync(int modelVozilaId, int poslovnicaId, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<ModelVozila>(modelVozilaId, "Model vozila", ct);
        await ObaveznoPostojiAsync<Poslovnica>(poslovnicaId, "Poslovnica", ct);
    }

    /// <summary>
    /// Gornja granica je tekuca godina plus jedna, jer se novi modeli prodaju kao
    /// sljedece godiste. Anotacija to ne moze izraziti - ona prima konstantu, a ova
    /// granica se pomjera svake godine.
    /// </summary>
    private static void ProvjeriGodinu(int godina)
    {
        var najvise = DateTime.UtcNow.Year + 1;

        if (godina > najvise)
        {
            throw new BusinessException($"Godina proizvodnje ne moze biti poslije {najvise}.");
        }
    }

    /// <summary>
    /// Dnevna tarifa mora biti veca od satne. Da nije, najam od 24 sata bio bi jeftiniji
    /// od najma od jednog sata, a PricingService bi uredno izracunao besmislicu.
    /// </summary>
    private static void ProvjeriTarife(decimal satna, decimal dnevna)
    {
        if (dnevna <= satna)
        {
            throw new BusinessException("Dnevna tarifa mora biti veca od satne.");
        }
    }

    private static string NormalizujOznaku(string oznaka) =>
        oznaka.Trim().ToUpperInvariant();
}
