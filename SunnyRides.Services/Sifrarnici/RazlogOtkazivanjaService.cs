using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class RazlogOtkazivanjaService
    : BaseCRUDService<RazlogOtkazivanjaDto, RazlogOtkazivanjaSearchObject, RazlogOtkazivanja,
                      RazlogOtkazivanjaInsertRequest, RazlogOtkazivanjaUpdateRequest>,
      IRazlogOtkazivanjaService
{
    public RazlogOtkazivanjaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Razlog otkazivanja";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<RazlogOtkazivanja> AddFilter(
        RazlogOtkazivanjaSearchObject search, IQueryable<RazlogOtkazivanja> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (search.ZaKlijenta.HasValue)
        {
            upit = upit.Where(x => x.ZaKlijenta == search.ZaKlijenta.Value);
        }

        if (search.ZaAgenciju.HasValue)
        {
            upit = upit.Where(x => x.ZaAgenciju == search.ZaAgenciju.Value);
        }

        if (search.Aktivan.HasValue)
        {
            upit = upit.Where(x => x.Aktivan == search.Aktivan.Value);
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Razlog otkazivanja sa tim nazivom vec postoji.";

    protected override Task BeforeInsertAsync(
        RazlogOtkazivanjaInsertRequest request, RazlogOtkazivanja entitet, CancellationToken ct)
    {
        ProvjeriKomeSeNudi(request.ZaKlijenta, request.ZaAgenciju);
        return Task.CompletedTask;
    }

    protected override Task BeforeUpdateAsync(
        RazlogOtkazivanjaUpdateRequest request, RazlogOtkazivanja entitet, CancellationToken ct)
    {
        ProvjeriKomeSeNudi(request.ZaKlijenta, request.ZaAgenciju);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Razlog koji je vec koristen ostaje u bazi, jer stare rezervacije pokazuju na njega.
    /// Ako vise ne treba, iskljuci se kroz izmjenu i nestane iz padajucih lista.
    /// </summary>
    protected override async Task BeforeDeleteAsync(RazlogOtkazivanja entitet, CancellationToken ct)
    {
        var brojRezervacija = await Context.Rezervacije
            .CountAsync(x => x.RazlogOtkazivanjaId == entitet.Id, ct);

        if (brojRezervacija > 0)
        {
            throw new BusinessException(
                $"Razlog \"{entitet.Naziv}\" je naveden na otkazanim rezervacijama ({brojRezervacija}), pa se ne moze obrisati. " +
                "Iskljucite ga umjesto brisanja i vise se nece nuditi.");
        }
    }

    private static void ProvjeriKomeSeNudi(bool zaKlijenta, bool zaAgenciju)
    {
        if (!zaKlijenta && !zaAgenciju)
        {
            throw new BusinessException(
                "Oznacite kome se razlog nudi: klijentu, agenciji ili obojici.");
        }
    }
}
