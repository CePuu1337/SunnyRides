using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class DrzavaService
    : BaseCRUDService<DrzavaDto, DrzavaSearchObject, Drzava, DrzavaInsertRequest, DrzavaUpdateRequest>,
      IDrzavaService
{
    public DrzavaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Drzava";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<Drzava> AddFilter(DrzavaSearchObject search, IQueryable<Drzava> upit)
    {
        // Filtriranje ide kroz Where uslov koji se prevede u SQL, ne ucitavanjem
        // svih zapisa u memoriju pa naknadnim LINQ filtriranjem.
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (!string.IsNullOrWhiteSpace(search.Skracenica))
        {
            upit = upit.Where(x => x.Skracenica.Contains(search.Skracenica));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Drzava sa tim nazivom vec postoji.";

    protected override async Task BeforeDeleteAsync(Drzava entitet, CancellationToken ct)
    {
        // Zapis koji drugi koriste se ne brise. Provjera je ovdje, prije EF-a, da
        // korisnik dobije razumljivu poruku umjesto greske iz baze.
        var brojGradova = await Context.Gradovi.CountAsync(x => x.DrzavaId == entitet.Id, ct);

        if (brojGradova > 0)
        {
            throw new BusinessException(
                $"Drzava \"{entitet.Naziv}\" se ne moze obrisati jer postoji {brojGradova} gradova u njoj.");
        }
    }
}
