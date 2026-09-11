using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class MarkaService
    : BaseCRUDService<MarkaDto, MarkaSearchObject, Marka, MarkaInsertRequest, MarkaUpdateRequest>,
      IMarkaService
{
    public MarkaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Marka";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<Marka> AddFilter(MarkaSearchObject search, IQueryable<Marka> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Marka sa tim nazivom vec postoji.";

    protected override async Task BeforeDeleteAsync(Marka entitet, CancellationToken ct)
    {
        var brojModela = await Context.ModeliVozila.CountAsync(x => x.MarkaId == entitet.Id, ct);

        if (brojModela > 0)
        {
            throw new BusinessException(
                $"Marka \"{entitet.Naziv}\" se ne moze obrisati jer postoje modeli te marke ({brojModela}).");
        }
    }
}
