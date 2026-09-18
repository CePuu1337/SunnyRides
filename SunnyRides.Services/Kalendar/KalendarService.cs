using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Database;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Kalendar;

public class KalendarService : IKalendarService
{
    /// <summary>
    /// Najduzi period koji se moze zatraziti. Kalendar je sedmicni pregled; trazenje
    /// godine unaprijed nije upotrebljivo na ekranu, a jeste nacin da se jednim
    /// zahtjevom povuce cijela historija flote.
    /// </summary>
    private const int MaksimalnoDana = 62;

    /// <summary>Gornja granica broja redova. Flota je manja, granica stoji da upit ostane ogranicen.</summary>
    private const int MaksimalnoVozila = 200;

    private readonly SunnyRidesDbContext _context;

    public KalendarService(SunnyRidesDbContext context)
    {
        _context = context;
    }

    public async Task<KalendarFloteDto> KalendarAsync(
        KalendarSearchObject search, CancellationToken ct = default)
    {
        var (od, doDatuma) = Period(search);

        var vozila = await VozilaAsync(search, ct);
        var voziloIds = vozila.Select(x => x.VoziloId).ToList();

        if (voziloIds.Count > 0)
        {
            await DodajRezervacijeAsync(vozila, voziloIds, od, doDatuma, ct);
            await DodajBlokadeAsync(vozila, voziloIds, od, doDatuma, ct);
        }

        foreach (var vozilo in vozila)
        {
            vozilo.Blokovi = vozilo.Blokovi.OrderBy(x => x.Od).ToList();
        }

        return new KalendarFloteDto
        {
            Od = od,
            Do = doDatuma,
            BufferSati = UslovDostupnosti.Buffer.TotalHours,
            Vozila = vozila
        };
    }

    /// <summary>
    /// Bez zadatog perioda kalendar pokazuje tekucu sedmicu, od ponedjeljka. To je
    /// podrazumijevani pogled na ekranu, pa ga server i vraca bez pitanja.
    /// </summary>
    private static (DateTime Od, DateTime Do) Period(KalendarSearchObject search)
    {
        var od = (search.Od ?? PocetakSedmice(DateTime.UtcNow)).Date;
        var doDatuma = (search.Do ?? od.AddDays(7)).Date;

        if (doDatuma <= od)
        {
            throw new BusinessException("Kraj perioda mora biti poslije pocetka.");
        }

        if ((doDatuma - od).TotalDays > MaksimalnoDana)
        {
            throw new BusinessException($"Period kalendara ne moze biti duzi od {MaksimalnoDana} dana.");
        }

        return (od, doDatuma);
    }

    private static DateTime PocetakSedmice(DateTime datum)
    {
        var pomak = ((int)datum.DayOfWeek + 6) % 7;

        return datum.Date.AddDays(-pomak);
    }

    /// <summary>
    /// Redovi kalendara. Deaktivirana vozila se ne prikazuju - ona nisu u ponudi, pa im
    /// prazan red u kalendaru ne znaci "slobodno" nego "ne postoji".
    /// </summary>
    private async Task<List<KalendarVoziloDto>> VozilaAsync(
        KalendarSearchObject search, CancellationToken ct)
    {
        var upit = _context.Vozila.AsNoTracking().Where(x => x.Aktivno);

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipVozilaId == search.TipVozilaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Vozilo))
        {
            upit = upit.Where(x =>
                x.ModelVozila.Naziv.Contains(search.Vozilo)
                || x.ModelVozila.Marka.Naziv.Contains(search.Vozilo)
                || x.RegistarskaOznaka.Contains(search.Vozilo));
        }

        return await upit
            .OrderBy(x => x.Poslovnica.Naziv)
            .ThenBy(x => x.ModelVozila.Marka.Naziv)
            .ThenBy(x => x.ModelVozila.Naziv)
            .ThenBy(x => x.RegistarskaOznaka)
            .Take(MaksimalnoVozila)
            .Select(x => new KalendarVoziloDto
            {
                VoziloId = x.Id,
                Vozilo = x.ModelVozila.Marka.Naziv + " " + x.ModelVozila.Naziv,
                RegistarskaOznaka = x.RegistarskaOznaka,
                TipVozila = x.ModelVozila.TipVozila.Naziv,
                Poslovnica = x.Poslovnica.Naziv,
                ThumbnailUrl = x.Slike
                    .Where(s => s.JeGlavna)
                    .Select(s => s.PutanjaThumbnail)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// Rezervacije koje se preklapaju sa periodom.
    ///
    /// Uzimaju se potvrdjene i one koje cekaju placanje a jos drze termin - tacno ono
    /// sto <see cref="IAvailabilityService"/> smatra zauzecem za buduce termine.
    ///
    /// Uz njih se crtaju i zavrsene, jer se kalendar gleda i unazad: da se ne crtaju,
    /// prosla sedmica bi izgledala kao da flota nije radila. Otkazanih nema - one nikad
    /// nisu ni drzale termin, pa bi njihov blok tvrdio nesto sto se nije desilo.
    /// </summary>
    private async Task DodajRezervacijeAsync(
        List<KalendarVoziloDto> vozila, List<int> voziloIds,
        DateTime od, DateTime doDatuma, CancellationToken ct)
    {
        var sada = DateTime.UtcNow;

        var rezervacije = await _context.Rezervacije
            .AsNoTracking()
            .Where(x => voziloIds.Contains(x.VoziloId)
                        && x.DatumOd < doDatuma && x.DatumDo > od
                        && (x.Status == StatusRezervacije.Confirmed
                            || x.Status == StatusRezervacije.Completed
                            || (x.Status == StatusRezervacije.Pending
                                && x.DrziDo != null && x.DrziDo > sada)))
            .Select(x => new
            {
                x.VoziloId,
                Blok = new KalendarBlokDto
                {
                    Vrsta = VrstaBlokaKalendara.Rezervacija,
                    Od = x.DatumOd,
                    Do = x.DatumDo,
                    RezervacijaId = x.Id,
                    Broj = x.Broj,
                    Klijent = x.Korisnik.Ime + " " + x.Korisnik.Prezime,
                    Status = x.Status,
                    DrziDo = x.DrziDo
                }
            })
            .ToListAsync(ct);

        Rasporedi(vozila, rezervacije.Select(x => (x.VoziloId, x.Blok)));
    }

    /// <summary>
    /// Blokade se crtaju kao i rezervacije, jer za kalendar znace isto: vozilo se tada
    /// ne moze izdati.
    /// </summary>
    private async Task DodajBlokadeAsync(
        List<KalendarVoziloDto> vozila, List<int> voziloIds,
        DateTime od, DateTime doDatuma, CancellationToken ct)
    {
        var blokade = await _context.BlokadeVozila
            .AsNoTracking()
            .Where(x => voziloIds.Contains(x.VoziloId) && x.DatumOd < doDatuma && x.DatumDo > od)
            .Select(x => new
            {
                x.VoziloId,
                Blok = new KalendarBlokDto
                {
                    Vrsta = VrstaBlokaKalendara.Blokada,
                    Od = x.DatumOd,
                    Do = x.DatumDo,
                    BlokadaId = x.Id,
                    Razlog = x.Razlog
                }
            })
            .ToListAsync(ct);

        Rasporedi(vozila, blokade.Select(x => (x.VoziloId, x.Blok)));
    }

    /// <summary>
    /// Raspoređuje blokove u redove po vozilu kroz rjecnik, umjesto pretragom liste za
    /// svaki blok. Pri trideset vozila razlika je nevidljiva, ali je ovo navika koja
    /// sprjecava da se pri vecoj floti pojavi tiho sporo mjesto.
    /// </summary>
    private static void Rasporedi(
        List<KalendarVoziloDto> vozila, IEnumerable<(int VoziloId, KalendarBlokDto Blok)> blokovi)
    {
        var poVozilu = vozila.ToDictionary(x => x.VoziloId);

        foreach (var (voziloId, blok) in blokovi)
        {
            if (poVozilu.TryGetValue(voziloId, out var red))
            {
                red.Blokovi.Add(blok);
            }
        }
    }
}
