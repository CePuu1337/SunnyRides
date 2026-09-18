using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Preporuke.Ml;

public class ModelPreporukeMf : IModelPreporuke
{
    /// <summary>
    /// Koliko dugo istreniran model vazi prije nego se sam osvjezi. Nove ocjene i najmovi
    /// do tada ne uticu na predikciju - to je cijena toga sto se model ne trenira pri
    /// svakom zahtjevu.
    /// </summary>
    private static readonly TimeSpan TrajanjeModela = TimeSpan.FromHours(6);

    /// <summary>
    /// Kandidati za broj latentnih faktora.
    ///
    /// Namjerno mali brojevi. Rang 8 nad dvanaest korisnika i deset modela vozila znaci
    /// blizu dvjesta parametara koji se uce iz pedesetak redova - model tada nauci skup
    /// za ucenje napamet i promasi sve ostalo. Koji ce se uzeti ne odlucuje se unaprijed
    /// nego mjerenjem.
    /// </summary>
    private static readonly int[] Rangovi = { 1, 2, 4, 8 };

    /// <summary>
    /// Kandidati za jacinu regularizacije. Veca vrijednost vuce predikciju prema prosjeku
    /// i time cuva model od ucenja napamet, ali prejaka ga svede na jednu te istu
    /// vrijednost za sve.
    /// </summary>
    private static readonly double[] Lambde = { 0.01, 0.05, 0.1, 0.3 };

    private const int RangBezOdabira = 2;
    private const double LambdaBezOdabira = 0.1;

    private const int BrojIteracija = 100;

    /// <summary>Broj dijelova vanjske provjere - onoga sto mjeri gresku.</summary>
    private const int VanjskihDijelova = 5;

    /// <summary>Broj dijelova unutrasnje provjere - one koja bira parametre.</summary>
    private const int UnutrasnjihDijelova = 3;

    private readonly IServiceScopeFactory _fabrikaOpsega;
    private readonly ILogger<ModelPreporukeMf> _logger;
    private readonly SemaphoreSlim _brava = new(1, 1);
    private readonly MLContext _ml;

    private ITransformer? _model;
    private HashSet<int> _korisnici = new();
    private HashSet<int> _modeli = new();
    private double _prosjekFlote = BodovanjePreporuke.NeutralnaOcjena;
    private DateTime? _treniran;

    public ModelPreporukeMf(IServiceScopeFactory fabrikaOpsega, ILogger<ModelPreporukeMf> logger)
    {
        _fabrikaOpsega = fabrikaOpsega;
        _logger = logger;

        // Fiksno sjeme: dva treniranja nad istim podacima daju isti model, pa je promjena
        // rezultata posljedica podataka a ne slucaja.
        _ml = new MLContext(seed: 0);

        Stanje = NemaModela("Model jos nije treniran.");
    }

    public StanjeModelaDto Stanje { get; private set; }

    public bool ZnaKorisnika(int korisnikId) => _model is not null && _korisnici.Contains(korisnikId);

    public async Task<StanjeModelaDto> OsvjeziAkoTrebaAsync(CancellationToken ct = default)
    {
        if (_treniran is not null && DateTime.UtcNow - _treniran.Value < TrajanjeModela)
        {
            return Stanje;
        }

        return await TrenirajAsync(ct);
    }

    public async Task<StanjeModelaDto> TrenirajAsync(CancellationToken ct = default)
    {
        await _brava.WaitAsync(ct);
        try
        {
            var sat = Stopwatch.StartNew();
            var redovi = await UcitajInterakcijeAsync(ct);

            if (!PripremaInterakcija.DovoljnoZaTreniranje(redovi))
            {
                _model = null;
                _treniran = DateTime.UtcNow;

                Stanje = NemaModela(
                    $"Premalo podataka za treniranje - potrebno je najmanje " +
                    $"{PripremaInterakcija.NajmanjeOcjenaZaTreniranje} ocjena od najmanje " +
                    $"{PripremaInterakcija.NajmanjeKorisnikaZaTreniranje} korisnika. " +
                    $"Preporuke se racunaju rezervnim putem.");

                _logger.LogWarning("Model preporuke nije treniran: {Napomena}", Stanje.Napomena);

                return Stanje;
            }

            // Prvo mjerenje, pa tek onda konacni model. Mjeri se **postupak**, ne jedan
            // model: u svakom prolazu se parametri biraju iznova, i to bez ijednog
            // pogleda u dio koji se tada mjeri.
            var mjerenje = IzmjeriUgnijezdeno(redovi);

            // Konacni model uci na svemu, jer je svaki podatak vrijedan - a koliko taj
            // postupak grijesi vec je izmjereno iznad.
            var odabir = OdaberiParametre(redovi, UnutrasnjihDijelova);
            var model = Nauci(redovi, odabir.Rang, odabir.Lambda);

            // Skupovi se postavljaju prije modela. Zahtjev koji naidje usred zamjene tada
            // vidi ili stari model sa starim skupovima ili novi sa novim.
            _korisnici = redovi.Select(x => x.KorisnikId).ToHashSet();
            _modeli = redovi.Select(x => x.ModelVozilaId).ToHashSet();
            _model = model;
            _treniran = DateTime.UtcNow;

            Stanje = new StanjeModelaDto
            {
                Treniran = true,
                TreniranUtc = _treniran,
                BrojInterakcija = redovi.Count,
                BrojStvarnihOcjena = redovi.Count(x => x.JeStvarnaOcjena),
                BrojProcijenjenih = redovi.Count(x => !x.JeStvarnaOcjena),
                BrojKorisnika = _korisnici.Count,
                BrojModelaVozila = _modeli.Count,
                BrojIteracija = BrojIteracija,
                Rang = odabir.Rang,
                Lambda = odabir.Lambda,
                RmseOdabira = odabir.Rmse,
                Rmse = mjerenje?.Rmse,
                Mae = mjerenje?.Mae,
                RKvadrat = mjerenje?.RKvadrat,
                RmseOsnovni = mjerenje?.RmseOsnovni,
                BrojMjerenja = mjerenje?.BrojMjerenja ?? 0,
                Napomena = mjerenje is null
                    ? "Premalo ocjena za unakrsnu provjeru, greska nije izmjerena."
                    : $"Greska je izmjerena ugnijezdenom unakrsnom provjerom: svaka od " +
                      $"{mjerenje.BrojMjerenja} ocjena tacno jednom je bila u skupu za provjeru, " +
                      $"a parametri su birani bez uvida u taj dio."
            };

            _logger.LogInformation(
                "Model preporuke treniran za {Trajanje} ms: {Redova} interakcija, {Korisnika} korisnika, " +
                "{Modela} modela vozila, rang {Rang}, lambda {Lambda}, RMSE {Rmse} (prosjek daje {Osnovni}), R2 {RKvadrat}.",
                sat.ElapsedMilliseconds, redovi.Count, _korisnici.Count, _modeli.Count,
                odabir.Rang, odabir.Lambda,
                mjerenje?.Rmse.ToString("0.000") ?? "nije mjeren",
                mjerenje?.RmseOsnovni.ToString("0.000") ?? "nije mjeren",
                mjerenje?.RKvadrat.ToString("0.000") ?? "nije mjeren");

            return Stanje;
        }
        finally
        {
            _brava.Release();
        }
    }

    public IReadOnlyDictionary<int, double> PredvidiOcjene(
        int korisnikId, IReadOnlyCollection<int> modelVozilaIds)
    {
        var model = _model;

        if (model is null || modelVozilaIds.Count == 0 || !_korisnici.Contains(korisnikId))
        {
            return new Dictionary<int, double>();
        }

        var ulaz = modelVozilaIds
            .Select(id => new InterakcijaZapis(korisnikId, id, 0, JeStvarnaOcjena: false))
            .ToList();

        var predikcije = Predvidi(model, ulaz);

        var rezultat = new Dictionary<int, double>(ulaz.Count);

        for (var i = 0; i < ulaz.Count; i++)
        {
            var modelVozilaId = ulaz[i].ModelVozilaId;

            // Model vozila kojeg u ucenju nije bilo dobija prosjek flote. Za njega
            // predikcija nema osnova, pa je neutralna vrijednost postenija.
            rezultat[modelVozilaId] = _modeli.Contains(modelVozilaId)
                ? predikcije[i]
                : _prosjekFlote;
        }

        return rezultat;
    }

    // --- mjerenje ----------------------------------------------------------

    /// <summary>
    /// Ugnijezdena unakrsna provjera.
    ///
    /// Podaci se dijele na pet dijelova. Svaki dio jednom bude skup za provjeru, a nad
    /// preostala cetiri se - jos jednom unakrsnom provjerom - biraju parametri, nauci
    /// model i predvide ocjene za taj dio.
    ///
    /// Zasto ovako, a ne jedan izdvojen skup: uz sedamdesetak ocjena jedan izdvojen skup
    /// ima trinaestak redova, a greska izmjerena na trinaest redova nije mjera kvaliteta
    /// nego stvar slucaja - zna ispasti dvostruko veca ili manja od stvarne. Ovako svaka
    /// ocjena tacno jednom posluzi za mjerenje, pa je rezultat izracunat nad svima.
    ///
    /// Predikcije iz svih prolaza se spajaju i mjere zajedno. Prosjek R kvadrata po malim
    /// skupovima ne bi bio isto sto i R kvadrat nad svim mjerenjima.
    /// </summary>
    private RezultatMjerenja? IzmjeriUgnijezdeno(IReadOnlyList<InterakcijaZapis> redovi)
    {
        var dijelovi = PripremaInterakcija.PodijeliUnakrsno(redovi, VanjskihDijelova);

        if (dijelovi.Count == 0)
        {
            return null;
        }

        var parovi = new List<(double Stvarno, double Predvidjeno)>();

        foreach (var (zaUcenje, zaProvjeru) in dijelovi)
        {
            var odabir = OdaberiParametre(zaUcenje, UnutrasnjihDijelova, tiho: true);
            var model = Nauci(zaUcenje, odabir.Rang, odabir.Lambda);
            var predikcije = Predvidi(model, zaProvjeru);

            for (var i = 0; i < zaProvjeru.Count; i++)
            {
                parovi.Add((zaProvjeru[i].Ocjena, predikcije[i]));
            }
        }

        return Mjere.Izracunaj(parovi);
    }

    /// <summary>
    /// Bira broj latentnih faktora i jacinu regularizacije mjerenjem, a ne pogadjanjem.
    ///
    /// Za svaki par se radi unakrsna provjera nad podacima za ucenje i uzima prosjecna
    /// greska; bira se par sa najmanjom. Kad podataka nema ni za to, uzimaju se umjerene
    /// vrijednosti - manji rang i osrednja regularizacija su sigurniji izbor na malo
    /// podataka.
    /// </summary>
    private (int Rang, double Lambda, double? Rmse) OdaberiParametre(
        IReadOnlyList<InterakcijaZapis> zaUcenje, int brojDijelova, bool tiho = false)
    {
        var dijelovi = PripremaInterakcija.PodijeliUnakrsno(zaUcenje, brojDijelova);

        if (dijelovi.Count == 0)
        {
            return (RangBezOdabira, LambdaBezOdabira, null);
        }

        var najboljiRang = RangBezOdabira;
        var najboljaLambda = LambdaBezOdabira;
        double? najmanjaGreska = null;

        foreach (var rang in Rangovi)
        {
            foreach (var lambda in Lambde)
            {
                var greske = new List<double>();

                foreach (var (ucenje, provjera) in dijelovi)
                {
                    var greska = GreskaNa(Nauci(ucenje, rang, lambda), provjera);

                    if (greska is not null)
                    {
                        greske.Add(greska.Value);
                    }
                }

                // Par koji nije dao rezultat na svakom dijelu se preskace - prosjek nad
                // razlicitim brojem mjerenja nije uporediv.
                if (greske.Count != dijelovi.Count)
                {
                    continue;
                }

                var prosjek = greske.Average();

                if (najmanjaGreska is null || prosjek < najmanjaGreska)
                {
                    najmanjaGreska = prosjek;
                    najboljiRang = rang;
                    najboljaLambda = lambda;
                }
            }
        }

        if (!tiho)
        {
            _logger.LogInformation(
                "Odabrani parametri modela: rang {Rang}, lambda {Lambda}, prosjecna greska " +
                "unakrsne provjere {Rmse} na {Dijelova} dijela.",
                najboljiRang, najboljaLambda, najmanjaGreska?.ToString("0.000") ?? "nije mjerena",
                dijelovi.Count);
        }

        return (najboljiRang, najboljaLambda, najmanjaGreska);
    }

    private double? GreskaNa(ITransformer model, IReadOnlyList<InterakcijaZapis> skup)
    {
        if (skup.Count == 0)
        {
            return null;
        }

        var predikcije = Predvidi(model, skup);

        var parovi = skup
            .Select((red, indeks) => (red.Ocjena, predikcije[indeks]))
            .ToList();

        return Mjere.Izracunaj(parovi).Rmse;
    }

    // --- ucenje i predikcija -----------------------------------------------

    /// <summary>Jedno treniranje sa zadatim parametrima.</summary>
    private ITransformer Nauci(IReadOnlyList<InterakcijaZapis> redovi, int rang, double lambda)
    {
        var podaci = _ml.Data.LoadFromEnumerable(redovi.Select(Pretvori).ToList());

        var opcije = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "KorisnikKljuc",
            MatrixRowIndexColumnName = "ModelKljuc",
            LabelColumnName = nameof(Interakcija.Ocjena),
            NumberOfIterations = BrojIteracija,
            ApproximationRank = rang,
            Lambda = lambda,

            // Bez ovoga trener ispisuje tok ucenja direktno na konzolu, mimo ILogger-a.
            Quiet = true
        };

        var postupak = _ml.Transforms.Conversion
            .MapValueToKey("KorisnikKljuc", nameof(Interakcija.KorisnikId))
            .Append(_ml.Transforms.Conversion.MapValueToKey("ModelKljuc", nameof(Interakcija.ModelVozilaId)))
            .Append(_ml.Recommendation().Trainers.MatrixFactorization(opcije));

        return postupak.Fit(podaci);
    }

    /// <summary>
    /// Predikcije za zadate redove, u istom redoslijedu.
    ///
    /// Vrijednost se svodi na raspon ocjene. Model po prirodi racuna neogranicen broj, pa
    /// bez toga zna predloziti "5,4 od 5" - a i greska bi se mjerila nad vrijednoscu koja
    /// se korisniku nikad ne prikazuje. Korisnik ili model vozila kojeg u ucenju nije
    /// bilo daje NaN i dobija prosjek flote: ni pohvalu ni kaznu, nego "o ovome se jos
    /// nista ne zna".
    ///
    /// Predikcija ide kroz Transform nad cijelim skupom odjednom. PredictionEngine nije
    /// siguran za istovremeno koristenje iz vise niti, a ovaj put jeste.
    /// </summary>
    private List<double> Predvidi(ITransformer model, IReadOnlyList<InterakcijaZapis> redovi)
    {
        var podaci = _ml.Data.LoadFromEnumerable(redovi.Select(Pretvori).ToList());
        var bodovano = model.Transform(podaci);

        return _ml.Data
            .CreateEnumerable<PredikcijaOcjene>(bodovano, reuseRowObject: false)
            .Select(x => float.IsNaN(x.Score)
                ? _prosjekFlote
                : Math.Clamp((double)x.Score, BodovanjePreporuke.NajmanjaOcjena, BodovanjePreporuke.NajvecaOcjena))
            .ToList();
    }

    // --- podaci ------------------------------------------------------------

    private async Task<List<InterakcijaZapis>> UcitajInterakcijeAsync(CancellationToken ct)
    {
        using var opseg = _fabrikaOpsega.CreateScope();
        var context = opseg.ServiceProvider.GetRequiredService<SunnyRidesDbContext>();

        // Skrivene recenzije ne ulaze u ucenje, isto kao sto ne ulaze ni u prosjecnu
        // ocjenu. Model ne smije uciti iz onoga sto je moderacija uklonila.
        var ocjene = await context.Recenzije
            .AsNoTracking()
            .Where(x => !x.Skrivena)
            .Select(x => new OcjenaZapis(x.KorisnikId, x.Vozilo.ModelVozilaId, x.Ocjena))
            .ToListAsync(ct);

        var najmovi = await context.Rezervacije
            .AsNoTracking()
            .Where(x => x.Status == StatusRezervacije.Completed)
            .Select(x => new NajamZapis(x.KorisnikId, x.Vozilo.ModelVozilaId))
            .ToListAsync(ct);

        _prosjekFlote = ocjene.Count > 0
            ? ocjene.Average(x => (double)x.Ocjena)
            : BodovanjePreporuke.NeutralnaOcjena;

        return PripremaInterakcija.Sastavi(ocjene, najmovi, _prosjekFlote);
    }

    private static StanjeModelaDto NemaModela(string napomena) => new()
    {
        Treniran = false,
        Napomena = napomena
    };

    private static Interakcija Pretvori(InterakcijaZapis zapis) => new()
    {
        KorisnikId = zapis.KorisnikId,
        ModelVozilaId = zapis.ModelVozilaId,
        Ocjena = (float)zapis.Ocjena
    };
}
