using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Flota;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Services.Rezervacije;

public class ZamjenaVozilaService : IZamjenaVozilaService
{
    /// <summary>
    /// Koliko zamjenskih vozila se nudi. Uposlenik bira jedno; lista je po prirodi
    /// kratka, a granica stoji jer endpoint bez limita uputstvo ne prihvata.
    /// </summary>
    private const int NajviseKandidata = 20;

    private readonly SunnyRidesDbContext _context;
    private readonly IAvailabilityService _dostupnost;
    private readonly IDozvolaService _dozvolaService;
    private readonly IRezervacijaStateMachine _stateMachine;
    private readonly IRezervacijaService _rezervacije;
    private readonly IObjavljivacPoruka _objavljivac;

    public ZamjenaVozilaService(
        SunnyRidesDbContext context,
        IAvailabilityService dostupnost,
        IDozvolaService dozvolaService,
        IRezervacijaStateMachine stateMachine,
        IRezervacijaService rezervacije,
        IObjavljivacPoruka objavljivac)
    {
        _context = context;
        _dostupnost = dostupnost;
        _dozvolaService = dozvolaService;
        _stateMachine = stateMachine;
        _rezervacije = rezervacije;
        _objavljivac = objavljivac;
    }

    public async Task<List<VoziloDto>> ZamjenskaVozilaAsync(
        int rezervacijaId, CancellationToken ct = default)
    {
        var rezervacija = await UcitajAsync(rezervacijaId, ct);
        ProvjeriDaSeMozeMijenjati(rezervacija);

        var staro = rezervacija.Vozilo;
        var kategorije = await _dozvolaService.DozvoljeneKategorijeIdAsync(
            rezervacija.KorisnikId, rezervacija.DatumOd, ct);

        var upit = _context.Vozila
            .AsNoTracking()
            .Where(x => x.Aktivno
                        && x.Id != staro.Id
                        && x.ModelVozila.TipVozilaId == staro.ModelVozila.TipVozilaId
                        && x.DnevnaTarifa >= staro.DnevnaTarifa
                        && x.Poslovnica.GradId == staro.Poslovnica.GradId
                        && kategorije.Contains(x.ModelVozila.KategorijaDozvoleId));

        // Ista provjera dostupnosti koju koriste pretraga i kreiranje rezervacije.
        upit = _dostupnost.DodajUslovSlobodno(upit, rezervacija.DatumOd, rezervacija.DatumDo);

        // Prvo vozila iz iste poslovnice - klijent ne mora nikud drugo - pa najblize
        // po cijeni, jer je najslicnije onome sto je rezervisao.
        var vozila = await upit
            .OrderByDescending(x => x.PoslovnicaId == staro.PoslovnicaId)
            .ThenBy(x => x.DnevnaTarifa)
            .ThenBy(x => x.Id)
            .Take(NajviseKandidata)
            .SaPovezanim()
            .ToListAsync(ct);

        var rezultat = vozila.Adapt<List<VoziloDto>>();
        await OcjeneVozila.DopuniAsync(_context, rezultat, ct);

        return rezultat;
    }

    /// <summary>
    /// Zamjena u jednoj transakciji.
    ///
    /// Redoslijed je isti kao kod kreiranja rezervacije: zakljucaju se rezervacija i
    /// novo vozilo, pa se tek onda provjerava dostupnost. Bez toga bi dvije zamjene, ili
    /// zamjena i nova rezervacija, mogle istovremeno uzeti isto vozilo za isti termin.
    /// </summary>
    public async Task<RezervacijaDto> ZamijeniAsync(
        int rezervacijaId, ZamjenaVozilaRequest request, CancellationToken ct = default)
    {
        await using var transakcija = await _context.Database.BeginTransactionAsync(ct);

        await _context.ZakljucajRezervacijuAsync(rezervacijaId, ct);
        await _dostupnost.ZakljucajVoziloAsync(request.NovoVoziloId, ct);

        var rezervacija = await UcitajZaIzmjenuAsync(rezervacijaId, ct);
        ProvjeriDaSeMozeMijenjati(rezervacija);

        var staro = rezervacija.Vozilo;

        var novo = await _context.Vozila
            .Include(x => x.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.Poslovnica)
            .FirstOrDefaultAsync(x => x.Id == request.NovoVoziloId, ct)
            ?? throw new BusinessException(
                $"Vozilo sa identifikatorom {request.NovoVoziloId} ne postoji.");

        ProvjeriZamjensko(staro, novo);

        // Klijent mora smjeti voziti novo vozilo - ista provjera kao pri rezervaciji,
        // na datum preuzimanja.
        await _dozvolaService.ObaveznoSmijeVozitiAsync(
            rezervacija.KorisnikId, novo.Id, rezervacija.DatumOd, ct);

        await _dostupnost.ObaveznoSlobodnoAsync(
            novo.Id, rezervacija.DatumOd, rezervacija.DatumDo, ct: ct);

        var napomena = string.IsNullOrWhiteSpace(request.Napomena) ? null : request.Napomena.Trim();

        _stateMachine.ZabiljeziIzmjenu(
            rezervacija,
            $"Vozilo zamijenjeno: {Opis(staro)} -> {Opis(novo)}. Cijena rezervacije je ostala ista.",
            napomena);

        // I kljuc i navigacija, da EF pri upisu ne mora birati kome da vjeruje.
        rezervacija.VoziloId = novo.Id;
        rezervacija.Vozilo = novo;

        // Vozilo se preuzima tamo gdje stoji, pa poslovnica prati novo vozilo.
        rezervacija.PoslovnicaId = novo.PoslovnicaId;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException(
                "Klijent vec ima rezervaciju tog vozila sa istim pocetkom. Odaberite drugo vozilo.", ex);
        }

        await transakcija.CommitAsync(ct);

        // Poruka ide tek kad je zamjena trajno upisana. Worker iz nje pravi obavjestenje
        // i email klijentu.
        await _objavljivac.ObjaviAsync(Redovi.VoziloZamijenjeno,
            new ZamjenaVozilaPoruka(rezervacija.Id, staro.Id), ct);

        return await _rezervacije.GetByIdAsync(rezervacija.Id, ct);
    }

    // --- pravila -----------------------------------------------------------

    /// <summary>
    /// Mijenja se samo rezervacija koja jos vazi i cije vozilo jos nije izdato. Izdato
    /// vozilo je fizicki kod klijenta; tu nema sta zamijeniti u sistemu, nego se vozilo
    /// vraca kroz primopredaju.
    /// </summary>
    private static void ProvjeriDaSeMozeMijenjati(Rezervacija rezervacija)
    {
        if (rezervacija.Status is not (StatusRezervacije.Pending or StatusRezervacije.Confirmed))
        {
            throw new BusinessException(
                "Vozilo se moze zamijeniti samo na rezervaciji koja ceka placanje ili je potvrdjena.");
        }

        if (rezervacija.Primopredaje.Any(p => p.Tip == TipPrimopredaje.Izdavanje))
        {
            throw new BusinessException(
                "Vozilo je vec izdato klijentu, pa se na ovoj rezervaciji ne moze zamijeniti.");
        }

        if (rezervacija.DatumDo <= DateTime.UtcNow)
        {
            throw new BusinessException("Termin ove rezervacije je vec prosao.");
        }
    }

    private static void ProvjeriZamjensko(Vozilo staro, Vozilo novo)
    {
        var razlog = PravilaZamjeneVozila.RazlogOdbijanja(ZaPravila(staro), ZaPravila(novo));

        if (razlog is not null)
        {
            throw new BusinessException(razlog);
        }
    }

    private static VoziloZaZamjenu ZaPravila(Vozilo vozilo) => new(
        vozilo.Id, vozilo.Aktivno, vozilo.ModelVozila.TipVozilaId, vozilo.DnevnaTarifa, vozilo.Poslovnica.GradId);

    // --- ucitavanje --------------------------------------------------------

    private async Task<Rezervacija> UcitajAsync(int rezervacijaId, CancellationToken ct)
    {
        return await Upit()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == rezervacijaId, ct)
            ?? throw NotFoundException.Za("Rezervacija", rezervacijaId);
    }

    private async Task<Rezervacija> UcitajZaIzmjenuAsync(int rezervacijaId, CancellationToken ct)
    {
        return await Upit()
            .FirstOrDefaultAsync(x => x.Id == rezervacijaId, ct)
            ?? throw NotFoundException.Za("Rezervacija", rezervacijaId);
    }

    private IQueryable<Rezervacija> Upit() =>
        _context.Rezervacije
            .Include(x => x.Vozilo).ThenInclude(v => v.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.Vozilo).ThenInclude(v => v.Poslovnica)
            .Include(x => x.Primopredaje)
            .AsSplitQuery();

    private static string Opis(Vozilo vozilo) =>
        $"{vozilo.ModelVozila.Marka.Naziv} {vozilo.ModelVozila.Naziv} ({vozilo.RegistarskaOznaka})";
}
