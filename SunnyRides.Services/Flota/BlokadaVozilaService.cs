using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Flota;

/// <summary>
/// Periodi u kojima vozilo nije dostupno za najam.
///
/// Blokada je, sa stanovista dostupnosti, ista stvar kao potvrdjena rezervacija -
/// vozilo je zauzeto. Zato ce je AvailabilityService u fazi 9 citati istim upitom
/// kojim cita i rezervacije, a ne kao poseban slucaj.
/// </summary>
public class BlokadaVozilaService
    : BaseCRUDService<BlokadaVozilaDto, BlokadaVozilaSearchObject, BlokadaVozila,
                      BlokadaVozilaInsertRequest, BlokadaVozilaUpdateRequest>,
      IBlokadaVozilaService
{
    /// <summary>Blokada duza od godinu dana je gotovo sigurno greska u unosu datuma.</summary>
    private const int MaksimalnoTrajanjeDana = 365;

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IOpsegPoslovnice _opseg;

    /// <summary>Poslovnica uposlenika; lista blokada se suzava na vozila te poslovnice.</summary>
    private int? _ogranicenjeNaPoslovnicu;

    public BlokadaVozilaService(
        SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik, IOpsegPoslovnice opseg)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _opseg = opseg;
    }

    public override async Task<PagedResult<BlokadaVozilaDto>> GetAsync(
        BlokadaVozilaSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaPoslovnicu = await _opseg.OgranicenjeAsync(ct);

        return await base.GetAsync(search, ct);
    }

    public override async Task<BlokadaVozilaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var blokada = await base.GetByIdAsync(id, ct);

        await ObaveznoSvojeVoziloAsync(blokada.VoziloId, ct);

        return blokada;
    }

    protected override string NazivEntiteta => "Blokada vozila";

    protected override string PodrazumijevaniPoredak => "DatumOd";

    protected override IQueryable<BlokadaVozila> AddFilter(
        BlokadaVozilaSearchObject search, IQueryable<BlokadaVozila> upit)
    {
        if (_ogranicenjeNaPoslovnicu is { } poslovnicaUposlenika)
        {
            upit = upit.Where(x => x.Vozilo.PoslovnicaId == poslovnicaUposlenika);
        }

        if (search.VoziloId.HasValue)
        {
            upit = upit.Where(x => x.VoziloId == search.VoziloId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.RegistarskaOznaka))
        {
            upit = upit.Where(x => x.Vozilo.RegistarskaOznaka.Contains(search.RegistarskaOznaka));
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.Vozilo.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Razlog))
        {
            upit = upit.Where(x => x.Razlog.Contains(search.Razlog));
        }

        // Preklapanje perioda: dva intervala se preklapaju ako svaki pocinje prije
        // nego sto onaj drugi zavrsi. Isti oblik uslova koristi i provjera
        // dostupnosti, samo tamo jos sa bufferom za pripremu vozila.
        if (search.PeriodOd.HasValue)
        {
            upit = upit.Where(x => x.DatumDo > search.PeriodOd.Value);
        }

        if (search.PeriodDo.HasValue)
        {
            upit = upit.Where(x => x.DatumOd < search.PeriodDo.Value);
        }

        if (search.SamoAktivne == true)
        {
            var sada = DateTime.UtcNow;
            upit = upit.Where(x => x.DatumDo > sada);
        }

        return upit;
    }

    protected override IQueryable<BlokadaVozila> AddInclude(
        BlokadaVozilaSearchObject search, IQueryable<BlokadaVozila> upit) => SaPovezanim(upit);

    protected override IQueryable<BlokadaVozila> AddIncludeDetalji(IQueryable<BlokadaVozila> upit) =>
        SaPovezanim(upit);

    private static IQueryable<BlokadaVozila> SaPovezanim(IQueryable<BlokadaVozila> upit) =>
        upit.Include(x => x.Vozilo).ThenInclude(v => v.ModelVozila)
            .Include(x => x.Vozilo).ThenInclude(v => v.Poslovnica)
            .Include(x => x.KreiraoKorisnik);

    protected override async Task BeforeInsertAsync(
        BlokadaVozilaInsertRequest request, BlokadaVozila entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Vozilo>(request.VoziloId, "Vozilo", ct);
        await ObaveznoSvojeVoziloAsync(request.VoziloId, ct);
        ProvjeriPeriod(request.DatumOd, request.DatumDo);

        // Ko je blokadu evidentirao dolazi iskljucivo iz tokena. Zahtjev to polje
        // nema, pa se ni greskom ne moze preuzeti od klijenta.
        entitet.KreiraoKorisnikId = _trenutniKorisnik.ObaveznoKorisnikId();
        entitet.DatumKreiranja = DateTime.UtcNow;
    }

    protected override async Task BeforeUpdateAsync(
        BlokadaVozilaUpdateRequest request, BlokadaVozila entitet, CancellationToken ct)
    {
        await ObaveznoSvojeVoziloAsync(entitet.VoziloId, ct);
        ProvjeriPeriod(request.DatumOd, request.DatumDo);

        // KreiraoKorisnikId se pri izmjeni ne dira - blokada ostaje pripisana onome
        // ko ju je unio, cak i kad je kasnije neko drugi ispravi.
    }

    protected override async Task BeforeDeleteAsync(BlokadaVozila entitet, CancellationToken ct)
    {
        await ObaveznoSvojeVoziloAsync(entitet.VoziloId, ct);
    }

    /// <summary>Blokade zadaje i mijenja uposlenik poslovnice u kojoj vozilo stoji.</summary>
    private Task ObaveznoSvojeVoziloAsync(int voziloId, CancellationToken ct) =>
        _opseg.ObaveznoDozvoljenoVoziloAsync(voziloId, ct);

    private static void ProvjeriPeriod(DateTime od, DateTime doDatuma)
    {
        if (doDatuma <= od)
        {
            throw new BusinessException("Kraj blokade mora biti poslije pocetka.");
        }

        if ((doDatuma - od).TotalDays > MaksimalnoTrajanjeDana)
        {
            throw new BusinessException(
                $"Blokada ne moze trajati duze od {MaksimalnoTrajanjeDana} dana.");
        }
    }
}
