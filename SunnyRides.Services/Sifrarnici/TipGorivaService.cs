using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class TipGorivaService
    : BaseCRUDService<TipGorivaDto, TipGorivaSearchObject, TipGoriva,
                      TipGorivaInsertRequest, TipGorivaUpdateRequest>,
      ITipGorivaService
{
    public TipGorivaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Tip goriva";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<TipGoriva> AddFilter(
        TipGorivaSearchObject search, IQueryable<TipGoriva> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Tip goriva sa tim nazivom vec postoji.";

    protected override async Task BeforeDeleteAsync(TipGoriva entitet, CancellationToken ct)
    {
        var brojModela = await Context.ModeliVozila.CountAsync(x => x.TipGorivaId == entitet.Id, ct);

        if (brojModela > 0)
        {
            throw new BusinessException(
                $"Tip goriva \"{entitet.Naziv}\" se ne moze obrisati jer ga koriste modeli vozila ({brojModela}).");
        }
    }
}
