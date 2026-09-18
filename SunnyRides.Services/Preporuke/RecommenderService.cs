using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Preporuke.Ml;

namespace SunnyRides.Services.Preporuke;

public class RecommenderService : IRecommenderService
{
    /// <summary>
    /// Gornja granica broja vozila koja ulaze u bodovanje. Flota je desetak puta manja,
    /// pa granica nikad ne odsijeca stvarne kandidate - stoji da bi upit ostao ogranicen
    /// i kad flota naraste.
    /// </summary>
    private const int MaksimalnoKandidata = 500;

    /// <summary>Koliko zadnjih pretraga ulazi u profil. Starije govore o ukusu od prije.</summary>
    private const int MaksimalnoPretraga = 200;

    /// <summary>
    /// Zavrsen najam je jaci signal od pretrage - korisnik je to vozilo stvarno uzeo i
    /// platio, dok pretraga moze biti i puko razgledanje.
    /// </summary>
    private const double TezinaPretrage = 1;
    private const double TezinaZavrsenogNajma = 2;

    private const int PodrazumijevanoPreporuka = 5;
    private const int MaksimalnoPreporuka = 20;
    private const int PodrazumijevanoSlicnih = 4;
    private const int MaksimalnoSlicnih = 10;

    private readonly SunnyRidesDbContext _context;
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IDozvolaService _dozvolaService;
    private readonly IAvailabilityService _dostupnost;
    private readonly IModelPreporuke _model;

    public RecommenderService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IDozvolaService dozvolaService,
        IAvailabilityService dostupnost,
        IModelPreporuke model)
    {
        _context = context;
        _trenutniKorisnik = trenutniKorisnik;
        _dozvolaService = dozvolaService;
        _dostupnost = dostupnost;
        _model = model;
    }

    // --- preporuke ---------------------------------------------------------

    public async Task<PagedResult<PreporukaDto>> PreporuciAsync(
        PreporukaSearchObject search, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var kandidati = await KandidatiAsync(search, korisnikId, ct);

        if (kandidati.Count == 0)
        {
            return new PagedResult<PreporukaDto> { Items = new List<PreporukaDto>(), TotalCount = 0 };
        }

        // Model se osvjezava ovdje, a ne u pozadini: treniranje traje sekundu-dvije nad
        // ovolikim podacima, a poziv je bez posla kad je model svjez.
        await _model.OsvjeziAkoTrebaAsync(ct);

        var poredani = _model.ZnaKorisnika(korisnikId)
            ? PredvidiModelom(kandidati, korisnikId)
            : await RezervnimPutemAsync(kandidati, korisnikId, ct);

        // Jedan primjerak po modelu vozila. Flota ima po nekoliko vozila istog modela, a
        // predikcija se i racuna na nivou modela - bez ovoga bi prve tri preporuke lako
        // bile isti skuter tri puta, sa razlicitim registracijama.
        poredani = poredani
            .GroupBy(x => x.Vozilo.ModelVozilaId)
            .Select(g => g.First())
            .ToList();

        var velicina = Math.Clamp(search.PageSize ?? PodrazumijevanoPreporuka, 1, MaksimalnoPreporuka);
        var stranica = Math.Max(search.Page ?? 0, 0);

        var zaPrikaz = poredani.Skip(stranica * velicina).Take(velicina).ToList();

        return new PagedResult<PreporukaDto>
        {
            Items = await SastaviAsync(zaPrikaz, ct),
            TotalCount = search.IncludeTotalCount ? poredani.Count : null
        };
    }

    public async Task<List<PreporukaDto>> SlicnaVozilaAsync(
        int voziloId, int? broj = null, CancellationToken ct = default)
    {
        var polaziste = await _context.Vozila
            .AsNoTracking()
            .Where(x => x.Id == voziloId)
            .Select(x => new
            {
                x.Id,
                x.ModelVozilaId,
                x.ModelVozila.TipVozilaId,
                x.ModelVozila.MarkaId,
                x.Poslovnica.GradId,
                x.ModelVozila.Kubikaza,
                x.DnevnaTarifa
            })
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.Za("Vozilo", voziloId);

        // Profil se ovdje ne gradi iz historije korisnika nego iz vozila koje gleda:
        // svaki signal ima udio 1 na vrijednosti tog vozila. Racun slicnosti ostaje
        // isti, pa "slicno ovome" i "slicno vama" mjere istu stvar na istoj skali.
        var profil = new ProfilKorisnika(
            Tipovi: new Dictionary<int, double> { [polaziste.TipVozilaId] = 1 },
            Marke: new Dictionary<int, double> { [polaziste.MarkaId] = 1 },
            Gradovi: new Dictionary<int, double> { [polaziste.GradId] = 1 },
            KubikazniRazredi: new Dictionary<int, double>
            {
                [BodovanjePreporuke.RazredKubikaze(polaziste.Kubikaza)] = 1
            },
            ProsjecnaCijena: polaziste.DnevnaTarifa);

        var kandidati = await KandidatiAsync(new PreporukaSearchObject(), KorisnikIliNull(), ct);
        kandidati = kandidati.Where(x => x.VoziloId != voziloId).ToList();

        if (kandidati.Count == 0)
        {
            return new List<PreporukaDto>();
        }

        // Slicna vozila se namjerno **ne** racunaju modelom. Matricna faktorizacija uci
        // ukus korisnika, a ovdje se pita nesto drugo: koje vozilo lici na ovo. To je
        // poredjenje atributa, pa ide kroz racun slicnosti.
        var poredani = await BodujAsync(kandidati, profil, ct);

        // I ovdje po jedan primjerak po modelu - cetiri puta ista Vespa nije prijedlog.
        poredani = poredani
            .Where(x => x.Vozilo.ModelVozilaId != polaziste.ModelVozilaId)
            .GroupBy(x => x.Vozilo.ModelVozilaId)
            .Select(g => g.First())
            .ToList();

        var koliko = Math.Clamp(broj ?? PodrazumijevanoSlicnih, 1, MaksimalnoSlicnih);

        return await SastaviAsync(poredani.Take(koliko).ToList(), ct);
    }

    // --- kandidati ---------------------------------------------------------

    /// <summary>
    /// Vozila koja uopste smiju biti predlozena: aktivna, dozvoljena za korisnikove
    /// kategorije i slobodna u trazenom terminu.
    ///
    /// Filtriranje ide kroz isti <see cref="IAvailabilityService"/> i isti
    /// <see cref="IDozvolaService"/> koje koriste pretraga i kreiranje rezervacije.
    /// Da preporuke imaju vlastitu provjeru, klijentu bi se moglo ponuditi vozilo koje
    /// mu rezervacija odbije.
    /// </summary>
    private async Task<List<KandidatVozilo>> KandidatiAsync(
        PreporukaSearchObject search, int? korisnikId, CancellationToken ct)
    {
        var upit = _context.Vozila.AsNoTracking().Where(x => x.Aktivno);

        if (korisnikId.HasValue)
        {
            var kategorije = await _dozvolaService.DozvoljeneKategorijeIdAsync(
                korisnikId.Value, search.SlobodnoOd, ct);

            upit = upit.Where(x => kategorije.Contains(x.ModelVozila.KategorijaDozvoleId));
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.GradId.HasValue)
        {
            upit = upit.Where(x => x.Poslovnica.GradId == search.GradId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipVozilaId == search.TipVozilaId.Value);
        }

        if (search.SlobodnoOd.HasValue && search.SlobodnoDo.HasValue
            && search.SlobodnoDo.Value > search.SlobodnoOd.Value)
        {
            upit = _dostupnost.DodajUslovSlobodno(upit, search.SlobodnoOd.Value, search.SlobodnoDo.Value);
        }

        // Iz baze se vuku samo atributi koji ulaze u bodovanje. Puni zapisi sa slikama
        // i nazivima ucitavaju se tek za onih nekoliko vozila koja stvarno idu u odgovor.
        var redovi = await upit
            .OrderBy(x => x.Id)
            .Take(MaksimalnoKandidata)
            .Select(x => new
            {
                x.Id,
                x.ModelVozilaId,
                x.ModelVozila.TipVozilaId,
                x.ModelVozila.MarkaId,
                x.Poslovnica.GradId,
                x.ModelVozila.Kubikaza,
                x.DnevnaTarifa
            })
            .ToListAsync(ct);

        return redovi
            .Select(x => new KandidatVozilo(
                x.Id, x.ModelVozilaId, x.TipVozilaId, x.MarkaId, x.GradId, x.Kubikaza, x.DnevnaTarifa))
            .ToList();
    }

    // --- profil ------------------------------------------------------------

    /// <summary>
    /// Ukus korisnika iz dva izvora: sta je trazio i sta je stvarno iznajmio.
    ///
    /// Oba se citaju jednim upitom svaki, bez upita u petlji. Zavrseni najmovi nose
    /// dvostruku tezinu u odnosu na pretrage.
    /// </summary>
    private async Task<ProfilKorisnika> ProfilAsync(int korisnikId, CancellationToken ct)
    {
        var pretrage = await _context.HistorijaPretraga
            .AsNoTracking()
            .Where(x => x.KorisnikId == korisnikId)
            .OrderByDescending(x => x.DatumVrijeme)
            .Take(MaksimalnoPretraga)
            .Select(x => new { x.TipVozilaId, x.MarkaId, x.GradId, x.CijenaOd, x.CijenaDo })
            .ToListAsync(ct);

        var najmovi = await _context.Rezervacije
            .AsNoTracking()
            .Where(x => x.KorisnikId == korisnikId && x.Status == StatusRezervacije.Completed)
            .Select(x => new
            {
                x.Vozilo.ModelVozila.TipVozilaId,
                x.Vozilo.ModelVozila.MarkaId,
                x.Vozilo.Poslovnica.GradId,
                x.Vozilo.ModelVozila.Kubikaza,
                x.Vozilo.DnevnaTarifa
            })
            .ToListAsync(ct);

        var tipovi = new Dictionary<int, double>();
        var marke = new Dictionary<int, double>();
        var gradovi = new Dictionary<int, double>();
        var razredi = new Dictionary<int, double>();
        var cijene = new List<decimal>();

        foreach (var pretraga in pretrage)
        {
            Prebroji(tipovi, pretraga.TipVozilaId, TezinaPretrage);
            Prebroji(marke, pretraga.MarkaId, TezinaPretrage);
            Prebroji(gradovi, pretraga.GradId, TezinaPretrage);

            // Pretraga ne nosi kubikazu - u zahtjevu takvog polja nema, pa taj signal
            // dolazi iskljucivo iz zavrsenih najmova.
            var trazenaCijena = SredinaRaspona(pretraga.CijenaOd, pretraga.CijenaDo);
            if (trazenaCijena.HasValue)
            {
                cijene.Add(trazenaCijena.Value);
            }
        }

        foreach (var najam in najmovi)
        {
            Prebroji(tipovi, najam.TipVozilaId, TezinaZavrsenogNajma);
            Prebroji(marke, najam.MarkaId, TezinaZavrsenogNajma);
            Prebroji(gradovi, najam.GradId, TezinaZavrsenogNajma);
            Prebroji(razredi, BodovanjePreporuke.RazredKubikaze(najam.Kubikaza), TezinaZavrsenogNajma);

            cijene.Add(najam.DnevnaTarifa);
        }

        return new ProfilKorisnika(
            Udjeli(tipovi), Udjeli(marke), Udjeli(gradovi), Udjeli(razredi),
            cijene.Count > 0 ? cijene.Average() : null);
    }

    // --- bodovanje ---------------------------------------------------------

    /// <summary>
    /// Rezervni put: ponderisano poklapanje profila i popularnost vozila. Koristi se kad
    /// model o korisniku nema podataka, i za "slicna vozila", gdje se i ne pita za ukus
    /// korisnika nego za slicnost atributa.
    /// </summary>
    private async Task<List<Bodovano>> BodujAsync(
        IReadOnlyList<KandidatVozilo> kandidati, ProfilKorisnika profil, CancellationToken ct)
    {
        var ids = kandidati.Select(x => x.VoziloId).ToList();
        var granica = DateTime.UtcNow.AddDays(-BodovanjePreporuke.DanaZaPopularnost);
        var sada = DateTime.UtcNow;

        // Najmovi koji su u prozoru stvarno poceli. Otkazane rezervacije se ne broje -
        // one ne govore da je vozilo bilo trazeno nego da nije bilo uzeto.
        var najmovi = await _context.Rezervacije
            .AsNoTracking()
            .Where(x => ids.Contains(x.VoziloId)
                        && x.DatumOd >= granica && x.DatumOd <= sada
                        && (x.Status == StatusRezervacije.Confirmed
                            || x.Status == StatusRezervacije.Completed))
            .GroupBy(x => x.VoziloId)
            .Select(g => new { VoziloId = g.Key, Broj = g.Count() })
            .ToDictionaryAsync(x => x.VoziloId, x => x.Broj, ct);

        // Skrivene recenzije ne ulaze ni u prosjek ni u preporuke - to je i razlog zbog
        // kojeg se recenzija skriva umjesto da se brise.
        var ocjene = await _context.Recenzije
            .AsNoTracking()
            .Where(x => ids.Contains(x.VoziloId) && !x.Skrivena)
            .GroupBy(x => x.VoziloId)
            .Select(g => new { VoziloId = g.Key, Broj = g.Count(), Zbir = g.Sum(r => r.Ocjena) })
            .ToListAsync(ct);

        // Iz anonimnog oblika u imenovani par, da se dalje u racunu cita sta je sta.
        var ocjenePoVozilu = ocjene.ToDictionary(
            x => x.VoziloId, x => (Broj: x.Broj, Zbir: (double)x.Zbir));

        var prosjekFlote = await _context.Recenzije
            .AsNoTracking()
            .Where(x => !x.Skrivena)
            .AverageAsync(x => (double?)x.Ocjena, ct) ?? 0;

        var kontekst = new KontekstPopularnosti(
            najmovi.Count > 0 ? najmovi.Values.Max() : 0, prosjekFlote);

        return kandidati
            .Select(vozilo =>
            {
                var brojNajmova = najmovi.TryGetValue(vozilo.VoziloId, out var n) ? n : 0;
                // Vozilo bez ijedne recenzije daje par (0, 0), sto je tacno ono sto
                // Bayesov racun i ocekuje - tada ostaje samo prosjek flote.
                ocjenePoVozilu.TryGetValue(vozilo.VoziloId, out var ocjena);

                var signali = new SignaliPopularnosti(brojNajmova, ocjena.Broj, ocjena.Zbir);

                var rezultat = BodovanjePreporuke.Boduj(profil, vozilo, signali, kontekst);

                return new Bodovano(
                    vozilo, rezultat.Skor, MetodaPreporuke.RezervnaHeuristika, null, rezultat);
            })
            // Poredak po skoru, a kod jednakih po identifikatoru - da dva uzastopna
            // poziva ne vrate istu listu u drugom redoslijedu.
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Vozilo.VoziloId)
            .ToList();
    }

    /// <summary>Profil korisnika pa bodovanje rezervnim putem.</summary>
    private async Task<List<Bodovano>> RezervnimPutemAsync(
        IReadOnlyList<KandidatVozilo> kandidati, int korisnikId, CancellationToken ct)
    {
        var profil = await ProfilAsync(korisnikId, ct);

        return await BodujAsync(kandidati, profil, ct);
    }

    /// <summary>
    /// Predikcija istreniranog modela.
    ///
    /// Model radi na nivou modela vozila, pa svi primjerci istog modela dobijaju istu
    /// predvidjenu ocjenu - i zato se kasnije po modelu zadrzava samo jedan primjerak.
    /// Predvidjena ocjena je na skali 1-5 i ovdje se svodi na 0-1, da skor ostane
    /// uporediv sa rezervnim putem.
    /// </summary>
    private List<Bodovano> PredvidiModelom(IReadOnlyList<KandidatVozilo> kandidati, int korisnikId)
    {
        var modeli = kandidati.Select(x => x.ModelVozilaId).Distinct().ToList();
        var predikcije = _model.PredvidiOcjene(korisnikId, modeli);

        return kandidati
            .Select(vozilo =>
            {
                var ocjena = predikcije.TryGetValue(vozilo.ModelVozilaId, out var p)
                    ? p
                    : BodovanjePreporuke.NeutralnaOcjena;

                var skor = Math.Clamp(
                    (ocjena - BodovanjePreporuke.NajmanjaOcjena)
                    / (BodovanjePreporuke.NajvecaOcjena - BodovanjePreporuke.NajmanjaOcjena), 0, 1);

                return new Bodovano(vozilo, skor, MetodaPreporuke.MatricnaFaktorizacija, ocjena, null);
            })
            .OrderByDescending(x => x.Skor)
            .ThenBy(x => x.Vozilo.VoziloId)
            .ToList();
    }

    // --- sastavljanje odgovora ---------------------------------------------

    /// <summary>
    /// Puni zapisi se ucitavaju tek sada, samo za vozila koja idu u odgovor, i to
    /// jednim upitom. Redoslijed iz bodovanja se pritom mora sacuvati - baza ga ne
    /// poznaje.
    /// </summary>
    private async Task<List<PreporukaDto>> SastaviAsync(
        IReadOnlyList<Bodovano> odabrani, CancellationToken ct)
    {
        if (odabrani.Count == 0)
        {
            return new List<PreporukaDto>();
        }

        var ids = odabrani.Select(x => x.Vozilo.VoziloId).ToList();

        var vozila = await _context.Vozila
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Include(x => x.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipVozila)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipGoriva)
            .Include(x => x.ModelVozila).ThenInclude(m => m.KategorijaDozvole)
            .Include(x => x.Poslovnica).ThenInclude(p => p.Grad)
            .Include(x => x.Slike.Where(s => s.JeGlavna))
            .ToDictionaryAsync(x => x.Id, ct);

        var preporuke = new List<PreporukaDto>(odabrani.Count);

        foreach (var bodovano in odabrani)
        {
            if (!vozila.TryGetValue(bodovano.Vozilo.VoziloId, out var vozilo))
            {
                continue;
            }

            var dto = vozilo.Adapt<VoziloDto>();

            preporuke.Add(new PreporukaDto
            {
                Vozilo = dto,
                Metoda = bodovano.Metoda,
                Skor = Math.Round(bodovano.Skor, 4),
                PredvidjenaOcjena = bodovano.PredvidjenaOcjena is null
                    ? null
                    : Math.Round(bodovano.PredvidjenaOcjena.Value, 2),

                // Ova dva polja opisuju rezervni racun i na predikciji modela nemaju
                // znacenje, pa tada ostaju prazna umjesto da se popune izmisljenom nulom
                // koja bi izgledala kao izmjerena vrijednost.
                Slicnost = bodovano.Heuristika is null ? 0 : Math.Round(bodovano.Heuristika.Slicnost, 4),
                Popularnost = bodovano.Heuristika is null ? 0 : Math.Round(bodovano.Heuristika.Popularnost, 4),

                Obrazlozenje = Obrazlozi(bodovano, dto)
            });
        }

        return preporuke;
    }

    /// <summary>
    /// Recenica se gradi od onoga sto je stavku stvarno dovelo gore: predikcije modela,
    /// ili signala koji je najvise doprinio skoru na rezervnom putu. Zato uz vozilo
    /// nikad ne moze stajati razlog koji u racunu nije ucestvovao.
    /// </summary>
    private static string Obrazlozi(Bodovano bodovano, VoziloDto vozilo)
    {
        if (bodovano.Heuristika is null)
        {
            var ocjena = bodovano.PredvidjenaOcjena ?? BodovanjePreporuke.NeutralnaOcjena;

            return $"Model procjenjuje da biste ovo vozilo ocijenili sa {ocjena:0.0} od 5, " +
                   "prema ocjenama korisnika slicnog ukusa.";
        }

        return ObrazloziHeuristiku(bodovano.Heuristika, vozilo);
    }

    private static string ObrazloziHeuristiku(RezultatBodovanja rezultat, VoziloDto vozilo) =>
        rezultat.Signal switch
        {
            SignalPreporuke.Tip =>
                $"Najcesce birate {(vozilo.TipVozilaNaziv ?? "ovaj tip vozila").ToLowerInvariant()}.",

            SignalPreporuke.Cijena =>
                $"Cijena {vozilo.DnevnaTarifa:0.00} EUR po danu je u rangu koji obicno trazite.",

            SignalPreporuke.Lokacija =>
                $"Vozilo je u poslovnici {vozilo.PoslovnicaNaziv}, u gradu u kojem najcesce trazite.",

            SignalPreporuke.Marka =>
                $"Marku {vozilo.MarkaNaziv} ste vec birali.",

            SignalPreporuke.Kubikaza =>
                $"Kubikaza {vozilo.Kubikaza} cm3 odgovara vozilima koja ste ranije iznajmljivali.",

            SignalPreporuke.Ocjena =>
                rezultat.BrojOcjena > 0
                    ? $"Visoko ocijenjeno: {rezultat.BayesovaOcjena:0.0} od 5, iz {rezultat.BrojOcjena} recenzija."
                    : "Jedno od bolje ocijenjenih vozila u floti.",

            _ => rezultat.BrojNajmova > 0
                ? $"Jedno od najtrazenijih vozila: {rezultat.BrojNajmova} najmova u zadnja tri mjeseca."
                : "Preporuceno prema popularnosti u floti."
        };

    // --- pomocno -----------------------------------------------------------

    /// <summary>
    /// Bodovana stavka, bez obzira kojim putem je nastala.
    ///
    /// <c>Heuristika</c> je popunjena samo na rezervnom putu, a <c>PredvidjenaOcjena</c>
    /// samo kad je skor dosao iz modela. Po tome se u odgovoru uvijek zna sta je model
    /// naucio, a sta je izracunato pravilom.
    /// </summary>
    private record Bodovano(
        KandidatVozilo Vozilo,
        double Skor,
        MetodaPreporuke Metoda,
        double? PredvidjenaOcjena,
        RezultatBodovanja? Heuristika);

    private int? KorisnikIliNull() => _trenutniKorisnik.KorisnikId;


    private static void Prebroji(IDictionary<int, double> udjeli, int? vrijednost, double tezina)
    {
        if (vrijednost is null)
        {
            return;
        }

        udjeli[vrijednost.Value] = udjeli.TryGetValue(vrijednost.Value, out var trenutno)
            ? trenutno + tezina
            : tezina;
    }

    /// <summary>Pretvara brojace u udjele koji se sabiraju u 1.</summary>
    private static IReadOnlyDictionary<int, double> Udjeli(Dictionary<int, double> brojaci)
    {
        var ukupno = brojaci.Values.Sum();

        if (ukupno <= 0)
        {
            return new Dictionary<int, double>();
        }

        return brojaci.ToDictionary(x => x.Key, x => x.Value / ukupno);
    }

    private static decimal? SredinaRaspona(decimal? od, decimal? doCijene) =>
        (od, doCijene) switch
        {
            (not null, not null) => (od.Value + doCijene.Value) / 2,
            (not null, null) => od.Value,
            (null, not null) => doCijene.Value,
            _ => null
        };
}
