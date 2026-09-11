using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class PaketOsiguranjaService
    : BaseCRUDService<PaketOsiguranjaDto, PaketOsiguranjaSearchObject, PaketOsiguranja,
                      PaketOsiguranjaInsertRequest, PaketOsiguranjaUpdateRequest>,
      IPaketOsiguranjaService
{
    public PaketOsiguranjaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Paket osiguranja";

    protected override string PodrazumijevaniPoredak => "CijenaPoDanu";

    protected override IQueryable<PaketOsiguranja> AddFilter(
        PaketOsiguranjaSearchObject search, IQueryable<PaketOsiguranja> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Paket osiguranja sa tim nazivom vec postoji.";

    protected override async Task BeforeDeleteAsync(PaketOsiguranja entitet, CancellationToken ct)
    {
        var brojRezervacija = await Context.Rezervacije
            .CountAsync(x => x.PaketOsiguranjaId == entitet.Id, ct);

        if (brojRezervacija > 0)
        {
            throw new BusinessException(
                $"Paket \"{entitet.Naziv}\" se ne moze obrisati jer je odabran na rezervacijama ({brojRezervacija}).");
        }
    }
}
