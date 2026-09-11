using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class PoslovnicaService
    : BaseCRUDService<PoslovnicaDto, PoslovnicaSearchObject, Poslovnica,
                      PoslovnicaInsertRequest, PoslovnicaUpdateRequest>,
      IPoslovnicaService
{
    public PoslovnicaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Poslovnica";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<Poslovnica> AddFilter(
        PoslovnicaSearchObject search, IQueryable<Poslovnica> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (!string.IsNullOrWhiteSpace(search.Adresa))
        {
            upit = upit.Where(x => x.Adresa.Contains(search.Adresa));
        }

        if (search.GradId.HasValue)
        {
            upit = upit.Where(x => x.GradId == search.GradId.Value);
        }

        // Poslovnica nema kolonu DrzavaId - uslov se penje kroz grad i ostaje
        // dio SQL upita, bez ucitavanja gradova u memoriju.
        if (search.DrzavaId.HasValue)
        {
            upit = upit.Where(x => x.Grad.DrzavaId == search.DrzavaId.Value);
        }

        return upit;
    }

    protected override IQueryable<Poslovnica> AddInclude(
        PoslovnicaSearchObject search, IQueryable<Poslovnica> upit) =>
        upit.Include(x => x.Grad).ThenInclude(g => g.Drzava);

    protected override IQueryable<Poslovnica> AddIncludeDetalji(IQueryable<Poslovnica> upit) =>
        upit.Include(x => x.Grad).ThenInclude(g => g.Drzava);

    protected override string PorukaZaDuplikat() =>
        "Poslovnica sa tim nazivom vec postoji.";

    protected override async Task BeforeInsertAsync(
        PoslovnicaInsertRequest request, Poslovnica entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Grad>(request.GradId, "Grad", ct);
        ProvjeriKoordinate(request.Latituda, request.Longituda);
    }

    protected override async Task BeforeUpdateAsync(
        PoslovnicaUpdateRequest request, Poslovnica entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Grad>(request.GradId, "Grad", ct);
        ProvjeriKoordinate(request.Latituda, request.Longituda);
    }

    protected override async Task BeforeDeleteAsync(Poslovnica entitet, CancellationToken ct)
    {
        var brojVozila = await Context.Vozila.CountAsync(x => x.PoslovnicaId == entitet.Id, ct);
        if (brojVozila > 0)
        {
            throw new BusinessException(
                $"Poslovnica \"{entitet.Naziv}\" se ne moze obrisati jer su joj dodijeljena vozila ({brojVozila}).");
        }

        var brojRezervacija = await Context.Rezervacije.CountAsync(x => x.PoslovnicaId == entitet.Id, ct);
        if (brojRezervacija > 0)
        {
            throw new BusinessException(
                $"Poslovnica \"{entitet.Naziv}\" se ne moze obrisati jer je vezana za rezervacije ({brojRezervacija}).");
        }

        var brojStanja = await Context.StanjaOpreme.CountAsync(x => x.PoslovnicaId == entitet.Id, ct);
        if (brojStanja > 0)
        {
            throw new BusinessException(
                $"Poslovnica \"{entitet.Naziv}\" se ne moze obrisati jer za nju postoje zalihe opreme ({brojStanja}).");
        }
    }

    /// <summary>
    /// Koordinate su opcione, ali idu u paru - poslovnica sa samo jednom koordinatom
    /// se na mapi ne moze prikazati, a podatak izgleda kao da postoji.
    /// </summary>
    private static void ProvjeriKoordinate(double? latituda, double? longituda)
    {
        if (latituda.HasValue != longituda.HasValue)
        {
            throw new BusinessException(
                "Koordinate se unose zajedno - potrebne su i latituda i longituda, ili nijedna.");
        }
    }
}
