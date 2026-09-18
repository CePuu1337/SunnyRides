using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Pregled;

public class PregledService : IPregledService
{
    private readonly SunnyRidesDbContext _context;

    public PregledService(SunnyRidesDbContext context)
    {
        _context = context;
    }

    public async Task<PregledPoslovanjaDto> PregledAsync(CancellationToken ct = default)
    {
        var sada = DateTime.UtcNow;
        var pocetakMjeseca = new DateTime(sada.Year, sada.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var pocetakDana = sada.Date;
        var krajDana = pocetakDana.AddDays(1);

        // Skup vozila koja su u ovom trenutku kod klijenata. Racuna se jednom i koristi
        // na tri mjesta - u metrikama i u oba presjeka - umjesto da se isti uslov pise
        // tri puta i tri puta izvrsava.
        var uNajmu = await _context.Rezervacije
            .AsNoTracking()
            .Where(x => x.Status == StatusRezervacije.Confirmed
                        && x.DatumOd <= sada && x.DatumDo >= sada
                        && x.Vozilo.Aktivno)
            .Select(x => new { x.VoziloId, x.Vozilo.ModelVozila.TipVozilaId, x.Vozilo.PoslovnicaId })
            .Distinct()
            .ToListAsync(ct);

        // Isti dio proslog mjeseca kao onaj koji je od ovog mjeseca protekao. Kraj se
        // odsijeca na pocetak tekuceg mjeseca da poredbeni period nikad ne zagazi u
        // njega - to se desava kad je prosli mjesec kraci od tekuceg.
        var prethodniOd = pocetakMjeseca.AddMonths(-1);
        var prethodniDo = prethodniOd.Add(sada - pocetakMjeseca);
        if (prethodniDo > pocetakMjeseca)
        {
            prethodniDo = pocetakMjeseca;
        }

        var metrike = await MetrikeAsync(sada, pocetakMjeseca, prethodniOd, prethodniDo, uNajmu.Count, ct);
        var raspored = await RasporedAsync(pocetakDana, krajDana, ct);
        var poTipu = await PoTipuAsync(uNajmu.Select(x => x.TipVozilaId).ToList(), ct);
        var poPoslovnici = await PoPoslovniciAsync(uNajmu.Select(x => x.PoslovnicaId).ToList(), ct);

        return new PregledPoslovanjaDto
        {
            NaDanUtc = sada,
            Metrike = metrike,
            RasporedDanas = raspored,
            PoTipuVozila = poTipu,
            PoPoslovnici = poPoslovnici
        };
    }

    // --- metrike -----------------------------------------------------------

    private async Task<MetrikePoslovanjaDto> MetrikeAsync(
        DateTime sada,
        DateTime pocetakMjeseca,
        DateTime prethodniOd,
        DateTime prethodniDo,
        int vozilaUNajmu,
        CancellationToken ct)
    {
        var aktivnihVozila = await _context.Vozila.CountAsync(x => x.Aktivno, ct);

        var aktivneRezervacije = await _context.Rezervacije
            .CountAsync(x => x.Status == StatusRezervacije.Confirmed && x.DatumDo >= sada, ct);

        // Prihod se racuna iz stvarno naplacenog iznosa, ne iz iznosa rezervacije.
        // Rezervacija nosi koliko je trebalo naplatiti, a placanje koliko jeste.
        var naplaceno = await _context.Placanja
            .Where(x => x.Status == StatusPlacanja.Succeeded && x.DatumKreiranja >= pocetakMjeseca)
            .SumAsync(x => (decimal?)x.NaplaceniIznos, ct) ?? 0m;

        // Povrat koji je odbijen ili ponisten nije novac koji je otisao, pa se ne broji.
        var refundirano = await _context.Refundi
            .Where(x => x.DatumKreiranja >= pocetakMjeseca
                        && x.Status != StatusPlacanja.Failed
                        && x.Status != StatusPlacanja.Canceled)
            .SumAsync(x => (decimal?)x.Iznos, ct) ?? 0m;

        var neverifikovaneDozvole = await _context.VozackeDozvole
            .CountAsync(x => x.Status == StatusDozvole.NaCekanju, ct);

        // Neplacene rezervacije koje jos drze termin. One kojima je drzanje isteklo
        // preuzima periodicni posao u workeru i osoblje s njima nema sta raditi.
        var neplaceneRezervacije = await _context.Rezervacije
            .CountAsync(x => x.Status == StatusRezervacije.Pending
                             && x.DrziDo != null && x.DrziDo > sada, ct);

        var poredba = await PoredbaAsync(
            sada, pocetakMjeseca, prethodniOd, prethodniDo, naplaceno - refundirano, ct);

        return new MetrikePoslovanjaDto
        {
            UkupnoAktivnihVozila = aktivnihVozila,
            VozilaUNajmu = vozilaUNajmu,
            TrenutnaIskoristenost = aktivnihVozila > 0
                ? Math.Round(100.0 * vozilaUNajmu / aktivnihVozila, 1)
                : 0,
            AktivneRezervacije = aktivneRezervacije,
            NaplacenoTekucegMjeseca = naplaceno,
            RefundiranoTekucegMjeseca = refundirano,
            NetoPrihodTekucegMjeseca = naplaceno - refundirano,
            CekaObradu = neverifikovaneDozvole + neplaceneRezervacije,
            NeverifikovaneDozvole = neverifikovaneDozvole,
            NeplaceneRezervacije = neplaceneRezervacije,
            Poredba = poredba
        };
    }

    // --- poredjenje sa proslim mjesecom -------------------------------------

    /// <summary>
    /// Iste tri brojke za tekuci i za poredbeni period.
    ///
    /// Poredi se samo ono sto se kroz period akumulira - novac i broj rezervacija.
    /// Iskoristenost i "ceka obradu" su trenutna stanja, ne zbirovi, pa za njih
    /// poredjenje sa proslim mjesecom nema znacenje i ovdje ih namjerno nema.
    /// </summary>
    private async Task<PoredbaPeriodaDto> PoredbaAsync(
        DateTime sada,
        DateTime pocetakMjeseca,
        DateTime prethodniOd,
        DateTime prethodniDo,
        decimal netoTekuci,
        CancellationToken ct)
    {
        var naplacenoPrethodno = await _context.Placanja
            .Where(x => x.Status == StatusPlacanja.Succeeded
                        && x.DatumKreiranja >= prethodniOd && x.DatumKreiranja < prethodniDo)
            .SumAsync(x => (decimal?)x.NaplaceniIznos, ct) ?? 0m;

        var refundiranoPrethodno = await _context.Refundi
            .Where(x => x.DatumKreiranja >= prethodniOd && x.DatumKreiranja < prethodniDo
                        && x.Status != StatusPlacanja.Failed
                        && x.Status != StatusPlacanja.Canceled)
            .SumAsync(x => (decimal?)x.Iznos, ct) ?? 0m;

        // Nove rezervacije se broje po datumu kreiranja, bez obzira na to za koji su
        // termin - to je mjera koliko je posla doslo u tom periodu.
        var noveTekuci = await _context.Rezervacije
            .CountAsync(x => x.DatumKreiranja >= pocetakMjeseca && x.DatumKreiranja < sada, ct);

        var novePrethodno = await _context.Rezervacije
            .CountAsync(x => x.DatumKreiranja >= prethodniOd && x.DatumKreiranja < prethodniDo, ct);

        // Zavrsene se broje po datumu vracanja, jer se najam tada i zavrsio.
        var zavrseneTekuci = await _context.Rezervacije
            .CountAsync(x => x.Status == StatusRezervacije.Completed
                             && x.DatumDo >= pocetakMjeseca && x.DatumDo < sada, ct);

        var zavrsenePrethodno = await _context.Rezervacije
            .CountAsync(x => x.Status == StatusRezervacije.Completed
                             && x.DatumDo >= prethodniOd && x.DatumDo < prethodniDo, ct);

        return new PoredbaPeriodaDto
        {
            TekuciOd = pocetakMjeseca,
            TekuciDo = sada,
            PrethodniOd = prethodniOd,
            PrethodniDo = prethodniDo,
            NetoPrihod = Metrika(netoTekuci, naplacenoPrethodno - refundiranoPrethodno),
            NoveRezervacije = Metrika(noveTekuci, novePrethodno),
            ZavrseneRezervacije = Metrika(zavrseneTekuci, zavrsenePrethodno)
        };
    }

    private static PoredbaMetrikaDto Metrika(decimal tekuce, decimal prethodno)
    {
        return new PoredbaMetrikaDto
        {
            Tekuce = tekuce,
            Prethodno = prethodno,

            // Dijeljenje nulom se ne zaobilazi nekom izmisljenom vrijednoscu. Kad
            // proslog mjeseca nije bilo nicega, postotak rasta ne postoji.
            PromjenaPosto = prethodno == 0m
                ? null
                : Math.Round((double)((tekuce - prethodno) / Math.Abs(prethodno)) * 100.0, 1)
        };
    }

    // --- raspored za danas -------------------------------------------------

    /// <summary>
    /// Preuzimanja i vracanja zakazana za danas, iz dva upita spojena u jednu listu.
    ///
    /// Dva upita a ne jedan, jer je rijec o dva razlicita uslova nad istom tabelom -
    /// jedan gleda datum preuzimanja, drugi datum vracanja, a ista rezervacija moze
    /// biti u oba (kratki najam unutar istog dana).
    /// </summary>
    private async Task<List<StavkaRasporedaDto>> RasporedAsync(
        DateTime pocetakDana, DateTime krajDana, CancellationToken ct)
    {
        var preuzimanja = await StavkeAsync(
            x => x.DatumOd >= pocetakDana && x.DatumOd < krajDana, TipPrimopredaje.Izdavanje, ct);

        var vracanja = await StavkeAsync(
            x => x.DatumDo >= pocetakDana && x.DatumDo < krajDana, TipPrimopredaje.Povrat, ct);

        return preuzimanja
            .Concat(vracanja)
            .OrderBy(x => x.Vrijeme)
            .ToList();
    }

    private async Task<List<StavkaRasporedaDto>> StavkeAsync(
        System.Linq.Expressions.Expression<Func<Database.Entities.Rezervacija, bool>> uslov,
        TipPrimopredaje tip,
        CancellationToken ct)
    {
        return await _context.Rezervacije
            .AsNoTracking()
            .Where(x => x.Status == StatusRezervacije.Confirmed || x.Status == StatusRezervacije.Completed)
            .Where(uslov)
            .Select(x => new StavkaRasporedaDto
            {
                RezervacijaId = x.Id,
                Broj = x.Broj,
                Tip = tip,
                Vrijeme = tip == TipPrimopredaje.Izdavanje ? x.DatumOd : x.DatumDo,
                Vozilo = x.Vozilo.ModelVozila.Marka.Naziv + " " + x.Vozilo.ModelVozila.Naziv,
                RegistarskaOznaka = x.Vozilo.RegistarskaOznaka,
                Klijent = x.Korisnik.Ime + " " + x.Korisnik.Prezime,
                Poslovnica = x.Poslovnica.Naziv,
                Status = x.Status,

                // Je li se to vec desilo. Uposlenik po ovome razlikuje sta ga jos ceka
                // od onoga sto je danas vec obavljeno.
                Evidentirano = x.Primopredaje.Any(p => p.Tip == tip)
            })
            .ToListAsync(ct);
    }

    // --- presjeci ----------------------------------------------------------

    private async Task<List<PresjekPoTipuDto>> PoTipuAsync(
        IReadOnlyList<int> tipoviUNajmu, CancellationToken ct)
    {
        var vozila = await _context.Vozila
            .AsNoTracking()
            .Where(x => x.Aktivno)
            .GroupBy(x => new { x.ModelVozila.TipVozilaId, x.ModelVozila.TipVozila.Naziv })
            .Select(g => new { g.Key.TipVozilaId, g.Key.Naziv, Broj = g.Count() })
            .ToListAsync(ct);

        return vozila
            .Select(x => new PresjekPoTipuDto
            {
                TipVozilaId = x.TipVozilaId,
                Naziv = x.Naziv,
                BrojVozila = x.Broj,
                UNajmu = tipoviUNajmu.Count(t => t == x.TipVozilaId)
            })
            .OrderByDescending(x => x.BrojVozila)
            .ToList();
    }

    private async Task<List<PresjekPoPoslovniciDto>> PoPoslovniciAsync(
        IReadOnlyList<int> poslovniceUNajmu, CancellationToken ct)
    {
        var vozila = await _context.Vozila
            .AsNoTracking()
            .Where(x => x.Aktivno)
            .GroupBy(x => new { x.PoslovnicaId, x.Poslovnica.Naziv })
            .Select(g => new { g.Key.PoslovnicaId, g.Key.Naziv, Broj = g.Count() })
            .ToListAsync(ct);

        return vozila
            .Select(x => new PresjekPoPoslovniciDto
            {
                PoslovnicaId = x.PoslovnicaId,
                Naziv = x.Naziv,
                BrojVozila = x.Broj,
                UNajmu = poslovniceUNajmu.Count(p => p == x.PoslovnicaId)
            })
            .OrderByDescending(x => x.BrojVozila)
            .ToList();
    }
}
