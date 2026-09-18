using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Database;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Izvjestaji.Dokumenti;

namespace SunnyRides.Services.Izvjestaji;

public class IzvjestajService : IIzvjestajService
{
    /// <summary>Najduzi period koji se moze zatraziti - dvije godine.</summary>
    private const int MaksimalnoDana = 732;

    /// <summary>Podrazumijevani period kad ga zahtjev ne navede: tekuca godina do danas.</summary>
    private const int PodrazumijevanoDana = 365;

    private readonly SunnyRidesDbContext _context;

    public IzvjestajService(SunnyRidesDbContext context)
    {
        _context = context;
    }

    // --- iskoristenost flote -----------------------------------------------

    public async Task<IskoristenostFloteDto> IskoristenostFloteAsync(
        IzvjestajSearchObject search, CancellationToken ct = default)
    {
        var (od, doDatuma) = Period(search);
        var poslovnica = await NazivPoslovniceAsync(search.PoslovnicaId, ct);

        var satiUPeriodu = (doDatuma - od).TotalHours;

        var najmovi = await NajmoviPoVoziluAsync(search.PoslovnicaId, od, doDatuma, ct);
        var prihodi = await PrihodPoVoziluAsync(search.PoslovnicaId, od, doDatuma, ct);
        var ocjene = await OcjenePoVoziluAsync(search.PoslovnicaId, ct);

        var sviVozila = await _context.Vozila
            .AsNoTracking()
            .Where(x => search.PoslovnicaId == null || x.PoslovnicaId == search.PoslovnicaId)
            .Select(x => new
            {
                x.Id,
                x.Aktivno,
                Vozilo = x.ModelVozila.Marka.Naziv + " " + x.ModelVozila.Naziv,
                x.RegistarskaOznaka,
                TipVozila = x.ModelVozila.TipVozila.Naziv,
                Poslovnica = x.Poslovnica.Naziv
            })
            .ToListAsync(ct);

        // U izvjestaj ulazi vozilo koje je **u ponudi**, i uz to svako koje je u periodu
        // imalo najam - pa i ako je u medjuvremenu povuceno.
        //
        // Razlog je racun, ne estetika: vozilo koje je odavno van ponude ima nula dana
        // izdato, a i dalje bi ulazilo u nazivnik i spustalo iskoristenost cijele flote.
        // Vozilo koje je radilo pa povuceno mora ostati, jer je zaradilo prihod koji bi
        // inace nestao iz izvjestaja.
        var vozila = sviVozila
            .Where(x => x.Aktivno || najmovi.ContainsKey(x.Id))
            .ToList();

        var stavke = vozila
            .Select(v =>
            {
                najmovi.TryGetValue(v.Id, out var najam);
                prihodi.TryGetValue(v.Id, out var prihod);
                ocjene.TryGetValue(v.Id, out var ocjena);

                var dana = Math.Round(najam.Sati / 24.0, 1);

                return new StavkaIskoristenostiDto
                {
                    VoziloId = v.Id,
                    Vozilo = v.Vozilo,
                    RegistarskaOznaka = v.RegistarskaOznaka,
                    TipVozila = v.TipVozila,
                    Poslovnica = v.Poslovnica,
                    BrojNajmova = najam.Broj,
                    DanaIzdato = dana,
                    Iskoristenost = Postotak(najam.Sati, satiUPeriodu),
                    Prihod = prihod,
                    ProsjecnaOcjena = ocjena.Broj > 0 ? Math.Round(ocjena.Zbir / ocjena.Broj, 2) : null,
                    BrojOcjena = ocjena.Broj
                };
            })
            .OrderByDescending(x => x.Iskoristenost)
            .ThenBy(x => x.RegistarskaOznaka)
            .ToList();

        return new IskoristenostFloteDto
        {
            Od = od,
            Do = doDatuma,
            Poslovnica = poslovnica,
            GenerisanoUtc = DateTime.UtcNow,
            Stavke = stavke,
            Zbir = Zbir(stavke, satiUPeriodu),
            PoTipuVozila = PoTipu(stavke, satiUPeriodu)
        };
    }

    /// <summary>
    /// Sati izdato i broj najmova po vozilu, jednim grupisanim upitom.
    ///
    /// Preklapanje najma sa periodom racuna baza: pocetak je kasniji od dva datuma, kraj
    /// raniji. Najam koji je poceo prije perioda ili se zavrsava poslije njega ulazi samo
    /// onim dijelom koji u period stvarno pada.
    /// </summary>
    private async Task<Dictionary<int, (int Broj, double Sati)>> NajmoviPoVoziluAsync(
        int? poslovnicaId, DateTime od, DateTime doDatuma, CancellationToken ct)
    {
        var redovi = await _context.Rezervacije
            .AsNoTracking()
            .Where(x => (x.Status == StatusRezervacije.Confirmed || x.Status == StatusRezervacije.Completed)
                        && x.DatumOd < doDatuma && x.DatumDo > od
                        && (poslovnicaId == null || x.PoslovnicaId == poslovnicaId))
            .GroupBy(x => x.VoziloId)
            .Select(g => new
            {
                VoziloId = g.Key,
                Broj = g.Count(),
                Sati = g.Sum(x => EF.Functions.DateDiffHour(
                    x.DatumOd > od ? x.DatumOd : od,
                    x.DatumDo < doDatuma ? x.DatumDo : doDatuma))
            })
            .ToListAsync(ct);

        return redovi.ToDictionary(x => x.VoziloId, x => (x.Broj, (double)x.Sati));
    }

    /// <summary>
    /// Prihod po vozilu iz **stvarno naplacenih** iznosa. Rezervacija nosi koliko je
    /// trebalo naplatiti, a placanje koliko jeste.
    /// </summary>
    private async Task<Dictionary<int, decimal>> PrihodPoVoziluAsync(
        int? poslovnicaId, DateTime od, DateTime doDatuma, CancellationToken ct)
    {
        var redovi = await _context.Placanja
            .AsNoTracking()
            .Where(x => x.Status == StatusPlacanja.Succeeded
                        && x.Rezervacija.DatumOd < doDatuma && x.Rezervacija.DatumDo > od
                        && (poslovnicaId == null || x.Rezervacija.PoslovnicaId == poslovnicaId))
            .GroupBy(x => x.Rezervacija.VoziloId)
            .Select(g => new { VoziloId = g.Key, Iznos = g.Sum(x => x.NaplaceniIznos ?? 0m) })
            .ToListAsync(ct);

        return redovi.ToDictionary(x => x.VoziloId, x => x.Iznos);
    }

    /// <summary>
    /// Ocjene po vozilu. Skrivene ne ulaze - isto pravilo koje vrijedi i za prosjecnu
    /// ocjenu u aplikaciji i za sistem preporuke.
    ///
    /// Ne ogranicava se na period: ocjena govori o vozilu, ne o mjesecu, pa bi sudjenje
    /// po tromjesecnom isjecku dalo prosjek iz dvije-tri recenzije.
    /// </summary>
    private async Task<Dictionary<int, (int Broj, double Zbir)>> OcjenePoVoziluAsync(
        int? poslovnicaId, CancellationToken ct)
    {
        var redovi = await _context.Recenzije
            .AsNoTracking()
            .Where(x => !x.Skrivena && (poslovnicaId == null || x.Vozilo.PoslovnicaId == poslovnicaId))
            .GroupBy(x => x.VoziloId)
            .Select(g => new { VoziloId = g.Key, Broj = g.Count(), Zbir = g.Sum(x => x.Ocjena) })
            .ToListAsync(ct);

        return redovi.ToDictionary(x => x.VoziloId, x => (x.Broj, (double)x.Zbir));
    }

    private static ZbirIskoristenostiDto Zbir(
        IReadOnlyList<StavkaIskoristenostiDto> stavke, double satiUPeriodu)
    {
        var saOcjenom = stavke.Where(x => x.BrojOcjena > 0).ToList();

        return new ZbirIskoristenostiDto
        {
            BrojVozila = stavke.Count,
            BrojNajmova = stavke.Sum(x => x.BrojNajmova),
            DanaIzdato = Math.Round(stavke.Sum(x => x.DanaIzdato), 1),
            Iskoristenost = Postotak(stavke.Sum(x => x.DanaIzdato) * 24, satiUPeriodu * stavke.Count),
            Prihod = stavke.Sum(x => x.Prihod),

            // Prosjek se racuna preko broja ocjena, ne preko vozila. Vozilo sa jednom
            // ocjenom inace vuce prosjek flote jednako kao ono sa cetrdeset.
            ProsjecnaOcjena = saOcjenom.Count > 0
                ? Math.Round(
                    saOcjenom.Sum(x => x.ProsjecnaOcjena!.Value * x.BrojOcjena) / saOcjenom.Sum(x => x.BrojOcjena), 2)
                : null
        };
    }

    private static List<IskoristenostPoTipuDto> PoTipu(
        IReadOnlyList<StavkaIskoristenostiDto> stavke, double satiUPeriodu)
    {
        return stavke
            .GroupBy(x => x.TipVozila)
            .Select(g => new IskoristenostPoTipuDto
            {
                TipVozila = g.Key,
                BrojVozila = g.Count(),
                BrojNajmova = g.Sum(x => x.BrojNajmova),
                DanaIzdato = Math.Round(g.Sum(x => x.DanaIzdato), 1),
                Iskoristenost = Postotak(g.Sum(x => x.DanaIzdato) * 24, satiUPeriodu * g.Count()),
                Prihod = g.Sum(x => x.Prihod)
            })
            .OrderByDescending(x => x.Prihod)
            .ToList();
    }

    // --- finansijski pregled -----------------------------------------------

    public async Task<FinansijskiPregledDto> FinansijskiPregledAsync(
        IzvjestajSearchObject search, CancellationToken ct = default)
    {
        var (od, doDatuma) = Period(search);
        var poslovnica = await NazivPoslovniceAsync(search.PoslovnicaId, ct);

        var naplate = await _context.Placanja
            .AsNoTracking()
            .Where(x => x.Status == StatusPlacanja.Succeeded
                        && x.DatumKreiranja >= od && x.DatumKreiranja < doDatuma
                        && (search.PoslovnicaId == null || x.Rezervacija.PoslovnicaId == search.PoslovnicaId))
            .GroupBy(x => new
            {
                x.DatumKreiranja.Year,
                x.DatumKreiranja.Month,
                x.Rezervacija.PoslovnicaId,
                Poslovnica = x.Rezervacija.Poslovnica.Naziv
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.PoslovnicaId,
                g.Key.Poslovnica,

                // Broj razlicitih rezervacija, ne broj placanja: jedna rezervacija moze
                // imati vise pokusaja naplate, a izvjestaj broji najmove.
                BrojRezervacija = g.Select(x => x.RezervacijaId).Distinct().Count(),
                Naplaceno = g.Sum(x => x.NaplaceniIznos ?? 0m)
            })
            .ToListAsync(ct);

        // Povrat koji je odbijen ili ponisten nije novac koji je otisao, pa se ne broji.
        var povrati = await _context.Refundi
            .AsNoTracking()
            .Where(x => x.DatumKreiranja >= od && x.DatumKreiranja < doDatuma
                        && x.Status != StatusPlacanja.Failed && x.Status != StatusPlacanja.Canceled
                        && (search.PoslovnicaId == null
                            || x.Placanje.Rezervacija.PoslovnicaId == search.PoslovnicaId))
            .GroupBy(x => new
            {
                x.DatumKreiranja.Year,
                x.DatumKreiranja.Month,
                x.Placanje.Rezervacija.PoslovnicaId
            })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.PoslovnicaId, Iznos = g.Sum(x => x.Iznos) })
            .ToListAsync(ct);

        var povratiPoKljucu = povrati.ToDictionary(
            x => (x.Year, x.Month, x.PoslovnicaId), x => x.Iznos);

        var stavke = naplate
            .Select(x =>
            {
                povratiPoKljucu.TryGetValue((x.Year, x.Month, x.PoslovnicaId), out var refundirano);

                return new StavkaFinansijskogDto
                {
                    Godina = x.Year,
                    Mjesec = x.Month,
                    Period = NazivMjeseca(x.Year, x.Month),
                    PoslovnicaId = x.PoslovnicaId,
                    Poslovnica = x.Poslovnica,
                    BrojRezervacija = x.BrojRezervacija,
                    Naplaceno = x.Naplaceno,
                    Refundirano = refundirano,
                    NetoPrihod = x.Naplaceno - refundirano,
                    ProsjecnaVrijednostNajma = x.BrojRezervacija > 0
                        ? Math.Round(x.Naplaceno / x.BrojRezervacija, 2)
                        : 0m
                };
            })
            .OrderBy(x => x.Godina)
            .ThenBy(x => x.Mjesec)
            .ThenBy(x => x.Poslovnica)
            .ToList();

        var ukupnoRezervacija = stavke.Sum(x => x.BrojRezervacija);
        var ukupnoNaplaceno = stavke.Sum(x => x.Naplaceno);
        var ukupnoRefundirano = stavke.Sum(x => x.Refundirano);

        return new FinansijskiPregledDto
        {
            Od = od,
            Do = doDatuma,
            Poslovnica = poslovnica,
            GenerisanoUtc = DateTime.UtcNow,
            Stavke = stavke,
            Zbir = new ZbirFinansijskogDto
            {
                BrojRezervacija = ukupnoRezervacija,
                Naplaceno = ukupnoNaplaceno,
                Refundirano = ukupnoRefundirano,
                NetoPrihod = ukupnoNaplaceno - ukupnoRefundirano,
                ProsjecnaVrijednostNajma = ukupnoRezervacija > 0
                    ? Math.Round(ukupnoNaplaceno / ukupnoRezervacija, 2)
                    : 0m
            }
        };
    }

    // --- PDF ---------------------------------------------------------------

    public async Task<byte[]> IskoristenostFlotePdfAsync(
        IzvjestajSearchObject search, CancellationToken ct = default)
    {
        var podaci = await IskoristenostFloteAsync(search, ct);

        return new IskoristenostFloteDokument(podaci).GeneratePdf();
    }

    public async Task<byte[]> FinansijskiPregledPdfAsync(
        IzvjestajSearchObject search, CancellationToken ct = default)
    {
        var podaci = await FinansijskiPregledAsync(search, ct);

        return new FinansijskiPregledDokument(podaci).GeneratePdf();
    }

    // --- pomocno -----------------------------------------------------------

    private static (DateTime Od, DateTime Do) Period(IzvjestajSearchObject search)
    {
        var doDatuma = (search.Do ?? DateTime.UtcNow.Date.AddDays(1)).Date;
        var od = (search.Od ?? doDatuma.AddDays(-PodrazumijevanoDana)).Date;

        if (doDatuma <= od)
        {
            throw new BusinessException("Kraj perioda mora biti poslije pocetka.");
        }

        if ((doDatuma - od).TotalDays > MaksimalnoDana)
        {
            throw new BusinessException($"Period izvjestaja ne moze biti duzi od {MaksimalnoDana} dana.");
        }

        return (od, doDatuma);
    }

    private async Task<string?> NazivPoslovniceAsync(int? poslovnicaId, CancellationToken ct)
    {
        if (poslovnicaId is null)
        {
            return null;
        }

        return await _context.Poslovnice
            .Where(x => x.Id == poslovnicaId.Value)
            .Select(x => x.Naziv)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessException($"Poslovnica sa identifikatorom {poslovnicaId} ne postoji.");
    }

    private static double Postotak(double dio, double cjelina) =>
        cjelina > 0 ? Math.Round(100 * dio / cjelina, 1) : 0;

    private static string NazivMjeseca(int godina, int mjesec)
    {
        string[] mjeseci =
        {
            "januar", "februar", "mart", "april", "maj", "juni",
            "juli", "august", "septembar", "oktobar", "novembar", "decembar"
        };

        var naziv = mjeseci[Math.Clamp(mjesec, 1, 12) - 1];

        return string.Create(CultureInfo.InvariantCulture, $"{naziv} {godina}");
    }
}
