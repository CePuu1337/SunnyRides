using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Dostupnost;

public class AvailabilityService : IAvailabilityService
{
    /// <summary>Isti limit kao za stranicu liste u baznom servisu.</summary>
    private const int NajvisePogodjenih = 100;

    private readonly SunnyRidesDbContext _context;

    public AvailabilityService(SunnyRidesDbContext context)
    {
        _context = context;
    }

    public IQueryable<Vozilo> DodajUslovSlobodno(
        IQueryable<Vozilo> upit, DateTime datumOd, DateTime datumDo)
    {
        var sada = DateTime.UtcNow;
        var granicaOd = UslovDostupnosti.GranicaOd(datumOd);
        var granicaDo = UslovDostupnosti.GranicaDo(datumDo);

        // Jedan upit sa dva NOT EXISTS uslova. Rezervacije i blokade se ne ucitavaju
        // u memoriju - baza sama odbacuje zauzeta vozila.
        return upit
            .Where(v => !v.Rezervacije.Any(r =>
                (r.Status == StatusRezervacije.Confirmed ||
                 (r.Status == StatusRezervacije.Pending && r.DrziDo > sada))
                && r.DatumDo > granicaOd
                && r.DatumOd < granicaDo))
            .Where(v => !v.Blokade.Any(b => b.DatumDo > datumOd && b.DatumOd < datumDo));
    }

    public async Task<bool> JeSlobodnoAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default)
    {
        ProvjeriPeriod(datumOd, datumDo);

        var imaRezervacija = await ZauzimajuceRezervacije(voziloId, datumOd, datumDo, ignorisiRezervacijuId)
            .AnyAsync(ct);

        if (imaRezervacija)
        {
            return false;
        }

        return !await ZauzimajuceBlokade(voziloId, datumOd, datumDo).AnyAsync(ct);
    }

    public async Task<DostupnostDto> ProvjeriAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default)
    {
        ProvjeriPeriod(datumOd, datumDo);

        var postoji = await _context.Vozila.AnyAsync(x => x.Id == voziloId, ct);
        if (!postoji)
        {
            throw NotFoundException.Za("Vozilo", voziloId);
        }

        var brojRezervacija = await ZauzimajuceRezervacije(voziloId, datumOd, datumDo, ignorisiRezervacijuId)
            .CountAsync(ct);

        var brojBlokada = await ZauzimajuceBlokade(voziloId, datumOd, datumDo).CountAsync(ct);

        return new DostupnostDto
        {
            VoziloId = voziloId,
            DatumOd = datumOd,
            DatumDo = datumDo,
            Slobodno = brojRezervacija == 0 && brojBlokada == 0,
            Razlog = Razlog(brojRezervacija, brojBlokada),
            BrojRezervacijaUTerminu = brojRezervacija,
            BrojBlokadaUTerminu = brojBlokada,
            BufferSati = UslovDostupnosti.Buffer.TotalHours
        };
    }

    public async Task ObaveznoSlobodnoAsync(
        int voziloId, DateTime datumOd, DateTime datumDo,
        int? ignorisiRezervacijuId = null, CancellationToken ct = default)
    {
        var dostupnost = await ProvjeriAsync(voziloId, datumOd, datumDo, ignorisiRezervacijuId, ct);

        if (!dostupnost.Slobodno)
        {
            throw new BusinessException(dostupnost.Razlog!);
        }
    }

    public async Task<List<int>> SlobodnaVozilaAsync(
        int? poslovnicaId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default)
    {
        ProvjeriPeriod(datumOd, datumDo);

        var upit = _context.Vozila.Where(x => x.Aktivno);

        if (poslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == poslovnicaId.Value);
        }

        return await DodajUslovSlobodno(upit, datumOd, datumDo)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<List<PogodjenaRezervacijaDto>> PogodjeneRezervacijeAsync(
        int voziloId, DateTime datumOd, DateTime datumDo, CancellationToken ct = default)
    {
        ProvjeriPeriod(datumOd, datumDo);

        // Gornja granica postoji iako je lista po prirodi kratka: endpoint bez limita
        // uputstvo ne prihvata. Poredak po pocetku znaci da prva pogodjena rezervacija,
        // po kojoj se blokada skracuje, uvijek ostaje u listi.
        var rezervacije = await ZauzimajuceRezervacije(voziloId, datumOd, datumDo, null)
            .Include(x => x.Korisnik)
            .OrderBy(x => x.DatumOd)
            .ThenBy(x => x.Id)
            .Take(NajvisePogodjenih)
            .AsNoTracking()
            .ToListAsync(ct);

        return rezervacije.Adapt<List<PogodjenaRezervacijaDto>>();
    }

    public async Task ZakljucajVoziloAsync(int voziloId, CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            // Lock bez transakcije se otpusta odmah po izvrsenju upita, pa ne stiti
            // nista. Ovo je greska u kodu koji poziva, ne stanje koje korisnik moze
            // izazvati - zato pada glasno, a ne kao poslovna greska.
            throw new InvalidOperationException(
                "Zakljucavanje vozila ima smisla samo unutar otvorene transakcije.");
        }

        // Jedini raw SQL u projektu. EF nema nacin da izrazi lock hint, a bez njega
        // dvije istovremene rezervacije mogu obje proci provjeru dostupnosti prije
        // nego ijedna upise svoj red.
        //
        // UPDLOCK uzima lock koji drugi citac sa istim hintom mora cekati; HOLDLOCK
        // ga drzi do kraja transakcije umjesto do kraja upita. Vrijednost ide kao
        // parametar, ne kao ulijepljeni tekst.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT TOP 1 Id FROM Vozilo WITH (UPDLOCK, HOLDLOCK) WHERE Id = {voziloId}", ct);
    }

    /// <summary>
    /// Rezervacije koje zauzimaju termin. Buffer se dodaje sa obje strane trazenog
    /// perioda, pa najam koji se zavrsava sat prije trazenog pocetka jos uvijek smeta.
    /// </summary>
    private IQueryable<Rezervacija> ZauzimajuceRezervacije(
        int voziloId, DateTime datumOd, DateTime datumDo, int? ignorisiRezervacijuId)
    {
        var sada = DateTime.UtcNow;
        var granicaOd = UslovDostupnosti.GranicaOd(datumOd);
        var granicaDo = UslovDostupnosti.GranicaDo(datumDo);

        var upit = _context.Rezervacije
            .Where(r => r.VoziloId == voziloId)

            // Otkazana rezervacija ne zauzima nista, a zavrsena je proslost.
            // Pending zauzima samo dok traje drzanje termina - kad istekne,
            // termin je slobodan i prije nego ga worker formalno otkaze.
            .Where(r => r.Status == StatusRezervacije.Confirmed
                        || (r.Status == StatusRezervacije.Pending && r.DrziDo > sada))

            .Where(r => r.DatumDo > granicaOd && r.DatumOd < granicaDo);

        if (ignorisiRezervacijuId.HasValue)
        {
            // Pri izmjeni termina postojece rezervacije ona ne smije smetati sama sebi.
            upit = upit.Where(r => r.Id != ignorisiRezervacijuId.Value);
        }

        return upit;
    }

    /// <summary>
    /// Blokade se gledaju bez buffera. Buffer postoji radi pripreme vozila izmedju
    /// dva najma; servis nije najam, pa se ovdje trazi samo stvarno preklapanje.
    /// </summary>
    private IQueryable<BlokadaVozila> ZauzimajuceBlokade(
        int voziloId, DateTime datumOd, DateTime datumDo) =>
        _context.BlokadeVozila
            .Where(b => b.VoziloId == voziloId)
            .Where(b => b.DatumDo > datumOd && b.DatumOd < datumDo);

    private static string? Razlog(int brojRezervacija, int brojBlokada)
    {
        if (brojBlokada > 0 && brojRezervacija > 0)
        {
            return "Vozilo je u tom terminu i rezervisano i blokirano.";
        }

        if (brojBlokada > 0)
        {
            return "Vozilo je u tom terminu blokirano (servis ili kvar).";
        }

        if (brojRezervacija > 0)
        {
            return "Vozilo je vec rezervisano u tom terminu ili prekratko prije njega.";
        }

        return null;
    }

    private static void ProvjeriPeriod(DateTime datumOd, DateTime datumDo)
    {
        if (datumDo <= datumOd)
        {
            throw new BusinessException("Datum vracanja mora biti poslije datuma preuzimanja.");
        }
    }
}
