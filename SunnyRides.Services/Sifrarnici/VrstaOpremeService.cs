using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class VrstaOpremeService
    : BaseCRUDService<VrstaOpremeDto, VrstaOpremeSearchObject, VrstaOpreme,
                      VrstaOpremeInsertRequest, VrstaOpremeUpdateRequest>,
      IVrstaOpremeService
{
    public VrstaOpremeService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Vrsta opreme";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<VrstaOpreme> AddFilter(
        VrstaOpremeSearchObject search, IQueryable<VrstaOpreme> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (search.SamoPoDanu == true)
        {
            upit = upit.Where(x => x.CijenaPoDanu != null);
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Oprema sa tim nazivom vec postoji.";

    protected override Task BeforeInsertAsync(
        VrstaOpremeInsertRequest request, VrstaOpreme entitet, CancellationToken ct)
    {
        ProvjeriCijenu(request.CijenaPoDanu, request.FiksnaCijena);
        return Task.CompletedTask;
    }

    protected override Task BeforeUpdateAsync(
        VrstaOpremeUpdateRequest request, VrstaOpreme entitet, CancellationToken ct)
    {
        ProvjeriCijenu(request.CijenaPoDanu, request.FiksnaCijena);
        return Task.CompletedTask;
    }

    protected override async Task BeforeDeleteAsync(VrstaOpreme entitet, CancellationToken ct)
    {
        var brojStavki = await Context.StavkeOpreme.CountAsync(x => x.VrstaOpremeId == entitet.Id, ct);
        if (brojStavki > 0)
        {
            throw new BusinessException(
                $"Oprema \"{entitet.Naziv}\" se ne moze obrisati jer se nalazi na rezervacijama ({brojStavki}).");
        }

        var brojStanja = await Context.StanjaOpreme.CountAsync(x => x.VrstaOpremeId == entitet.Id, ct);
        if (brojStanja > 0)
        {
            throw new BusinessException(
                $"Oprema \"{entitet.Naziv}\" se ne moze obrisati jer za nju postoje evidentirane zalihe po poslovnicama ({brojStanja}).");
        }
    }

    /// <summary>
    /// Oprema se naplacuje ili po danu ili fiksno, nikad oboje i nikad nijedno.
    /// PricingService kasnije bira granu po tome koje je polje postavljeno - ako
    /// bi obje bile popunjene, obracun bi zavisio od redoslijeda provjera u kodu,
    /// a to je tacno vrsta neodredjenosti koju u cijeni ne smijemo imati.
    /// </summary>
    private static void ProvjeriCijenu(decimal? cijenaPoDanu, decimal? fiksnaCijena)
    {
        if (cijenaPoDanu.HasValue && fiksnaCijena.HasValue)
        {
            throw new BusinessException(
                "Unesite ili cijenu po danu ili fiksnu cijenu, ne obje.");
        }

        if (!cijenaPoDanu.HasValue && !fiksnaCijena.HasValue)
        {
            throw new BusinessException(
                "Unesite cijenu po danu ili fiksnu cijenu.");
        }
    }
}
