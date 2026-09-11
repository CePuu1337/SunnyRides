using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class GradService
    : BaseCRUDService<GradDto, GradSearchObject, Grad, GradInsertRequest, GradUpdateRequest>,
      IGradService
{
    public GradService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Grad";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<Grad> AddFilter(GradSearchObject search, IQueryable<Grad> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (search.DrzavaId.HasValue)
        {
            upit = upit.Where(x => x.DrzavaId == search.DrzavaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.PostanskiBroj))
        {
            upit = upit.Where(x => x.PostanskiBroj != null
                                   && x.PostanskiBroj.Contains(search.PostanskiBroj));
        }

        return upit;
    }

    // Lista prikazuje naziv drzave uz svaki grad, pa se drzava ucitava jednim JOIN-om.
    // Bez ovoga bi Mapster za svaki red vidio praznu navigaciju, a dohvat naziva
    // u petlji bio bi N+1 upit.
    protected override IQueryable<Grad> AddInclude(GradSearchObject search, IQueryable<Grad> upit) =>
        upit.Include(x => x.Drzava);

    protected override IQueryable<Grad> AddIncludeDetalji(IQueryable<Grad> upit) =>
        upit.Include(x => x.Drzava);

    protected override string PorukaZaDuplikat() =>
        "Grad sa tim nazivom vec postoji u odabranoj drzavi.";

    protected override async Task BeforeInsertAsync(
        GradInsertRequest request, Grad entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Drzava>(request.DrzavaId, "Drzava", ct);
    }

    protected override async Task BeforeUpdateAsync(
        GradUpdateRequest request, Grad entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Drzava>(request.DrzavaId, "Drzava", ct);
    }

    protected override async Task BeforeDeleteAsync(Grad entitet, CancellationToken ct)
    {
        var brojPoslovnica = await Context.Poslovnice.CountAsync(x => x.GradId == entitet.Id, ct);

        if (brojPoslovnica > 0)
        {
            throw new BusinessException(
                $"Grad \"{entitet.Naziv}\" se ne moze obrisati jer u njemu postoje poslovnice ({brojPoslovnica}).");
        }
    }
}
