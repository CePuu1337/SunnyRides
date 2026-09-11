using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class TipVozilaService
    : BaseCRUDService<TipVozilaDto, TipVozilaSearchObject, TipVozila,
                      TipVozilaInsertRequest, TipVozilaUpdateRequest>,
      ITipVozilaService
{
    public TipVozilaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Tip vozila";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<TipVozila> AddFilter(
        TipVozilaSearchObject search, IQueryable<TipVozila> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Tip vozila sa tim nazivom vec postoji.";

    protected override async Task BeforeDeleteAsync(TipVozila entitet, CancellationToken ct)
    {
        var brojModela = await Context.ModeliVozila.CountAsync(x => x.TipVozilaId == entitet.Id, ct);
        if (brojModela > 0)
        {
            throw new BusinessException(
                $"Tip vozila \"{entitet.Naziv}\" se ne moze obrisati jer postoje modeli tog tipa ({brojModela}).");
        }

        var brojPravila = await Context.PravilaKategorija.CountAsync(x => x.TipVozilaId == entitet.Id, ct);
        if (brojPravila > 0)
        {
            throw new BusinessException(
                $"Tip vozila \"{entitet.Naziv}\" se ne moze obrisati jer se koristi u pravilima kategorija dozvola ({brojPravila}).");
        }
    }
}
