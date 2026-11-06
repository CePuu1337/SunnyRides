using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Oprema;

/// <summary>
/// Zalihe opreme po poslovnici: koliko kaciga, GPS uredjaja i ostalog poslovnica ima.
///
/// Administrator vodi zalihe svih poslovnica, uposlenik samo svoje - poslovnica se
/// uposleniku cita iz baze, kao i na ostalim ekranima osoblja.
///
/// Kolicina se ne moze spustiti ispod onoga sto je vec obecano rezervacijama. Inace
/// bi neko na papiru "otpisao" kacigu koju klijent u subotu dolazi preuzeti.
/// </summary>
public class StanjeOpremeService
    : BaseCRUDService<StanjeOpremeDto, StanjeOpremeSearchObject, StanjeOpreme,
                      StanjeOpremeInsertRequest, StanjeOpremeUpdateRequest>,
      IStanjeOpremeService
{
    private readonly IOpsegPoslovnice _opseg;
    private readonly IDostupnostOpremeService _dostupnost;

    /// <summary>Poslovnica uposlenika; lista se suzava na nju.</summary>
    private int? _ogranicenjeNaPoslovnicu;

    public StanjeOpremeService(
        SunnyRidesDbContext context, IOpsegPoslovnice opseg, IDostupnostOpremeService dostupnost)
        : base(context)
    {
        _opseg = opseg;
        _dostupnost = dostupnost;
    }

    protected override string NazivEntiteta => "Zaliha opreme";

    protected override string PodrazumijevaniPoredak => "PoslovnicaId";

    protected override string PorukaZaDuplikat() =>
        "Za tu opremu u toj poslovnici zalihe su vec evidentirane. Izmijenite postojeci zapis.";

    public override async Task<PagedResult<StanjeOpremeDto>> GetAsync(
        StanjeOpremeSearchObject search, CancellationToken ct = default)
    {
        _ogranicenjeNaPoslovnicu = await _opseg.OgranicenjeAsync(ct);

        return await base.GetAsync(search, ct);
    }

    public override async Task<StanjeOpremeDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var stanje = await base.GetByIdAsync(id, ct);

        await _opseg.ObaveznoDozvoljenaAsync(stanje.PoslovnicaId, ct);

        return stanje;
    }

    protected override IQueryable<StanjeOpreme> AddFilter(
        StanjeOpremeSearchObject search, IQueryable<StanjeOpreme> upit)
    {
        if (_ogranicenjeNaPoslovnicu is { } poslovnicaUposlenika)
        {
            upit = upit.Where(x => x.PoslovnicaId == poslovnicaUposlenika);
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.VrstaOpremeId.HasValue)
        {
            upit = upit.Where(x => x.VrstaOpremeId == search.VrstaOpremeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.VrstaOpreme.Naziv.Contains(search.Naziv));
        }

        return upit;
    }

    protected override IQueryable<StanjeOpreme> AddInclude(
        StanjeOpremeSearchObject search, IQueryable<StanjeOpreme> upit) => SaPovezanim(upit);

    protected override IQueryable<StanjeOpreme> AddIncludeDetalji(IQueryable<StanjeOpreme> upit) =>
        SaPovezanim(upit);

    private static IQueryable<StanjeOpreme> SaPovezanim(IQueryable<StanjeOpreme> upit) =>
        upit.Include(x => x.VrstaOpreme)
            .Include(x => x.Poslovnica);

    protected override async Task BeforeInsertAsync(
        StanjeOpremeInsertRequest request, StanjeOpreme entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<VrstaOpreme>(request.VrstaOpremeId, "Vrsta opreme", ct);
        await ObaveznoPostojiAsync<Poslovnica>(request.PoslovnicaId, "Poslovnica", ct);
        await _opseg.ObaveznoDozvoljenaAsync(request.PoslovnicaId, ct);
    }

    protected override async Task BeforeUpdateAsync(
        StanjeOpremeUpdateRequest request, StanjeOpreme entitet, CancellationToken ct)
    {
        await _opseg.ObaveznoDozvoljenaAsync(entitet.PoslovnicaId, ct);

        // Zakljucavanje prije provjere: rezervacija koja upravo uzima kacigu mora
        // ili zavrsiti prije ove izmjene, ili sacekati da se ona upise.
        await _dostupnost.ZakljucajZaliheAsync(entitet.PoslovnicaId, ct);
        await _dostupnost.ObaveznoPokrivaObecanoAsync(
            entitet.PoslovnicaId, entitet.VrstaOpremeId, request.Kolicina, ct);
    }

    protected override async Task BeforeDeleteAsync(StanjeOpreme entitet, CancellationToken ct)
    {
        await _opseg.ObaveznoDozvoljenaAsync(entitet.PoslovnicaId, ct);

        // Brisanje zapisa je isto sto i kolicina nula.
        await _dostupnost.ZakljucajZaliheAsync(entitet.PoslovnicaId, ct);
        await _dostupnost.ObaveznoPokrivaObecanoAsync(
            entitet.PoslovnicaId, entitet.VrstaOpremeId, 0, ct);
    }
}
