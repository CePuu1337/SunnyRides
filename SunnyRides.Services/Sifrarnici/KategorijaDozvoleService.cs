using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class KategorijaDozvoleService
    : BaseCRUDService<KategorijaDozvoleDto, KategorijaDozvoleSearchObject, KategorijaDozvole,
                      KategorijaDozvoleInsertRequest, KategorijaDozvoleUpdateRequest>,
      IKategorijaDozvoleService
{
    public KategorijaDozvoleService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Kategorija dozvole";

    protected override string PodrazumijevaniPoredak => "Oznaka";

    protected override IQueryable<KategorijaDozvole> AddFilter(
        KategorijaDozvoleSearchObject search, IQueryable<KategorijaDozvole> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Oznaka))
        {
            upit = upit.Where(x => x.Oznaka.Contains(search.Oznaka));
        }

        return upit;
    }

    protected override string PorukaZaDuplikat() =>
        "Kategorija sa tom oznakom vec postoji.";

    protected override Task BeforeInsertAsync(
        KategorijaDozvoleInsertRequest request, KategorijaDozvole entitet, CancellationToken ct)
    {
        // Oznaka se cuva velikim slovima, jer se po njoj poredi i prikazuje.
        // Bez normalizacije bi "a1" i "A1" prosli kao dva razlicita zapisa,
        // a jedinstveni indeks na SQL Serveru ih ne bi razlikovao tek kasnije.
        entitet.Oznaka = request.Oznaka.Trim().ToUpperInvariant();
        return Task.CompletedTask;
    }

    protected override Task BeforeUpdateAsync(
        KategorijaDozvoleUpdateRequest request, KategorijaDozvole entitet, CancellationToken ct)
    {
        entitet.Oznaka = request.Oznaka.Trim().ToUpperInvariant();
        return Task.CompletedTask;
    }

    protected override async Task BeforeDeleteAsync(KategorijaDozvole entitet, CancellationToken ct)
    {
        var brojModela = await Context.ModeliVozila.CountAsync(x => x.KategorijaDozvoleId == entitet.Id, ct);
        if (brojModela > 0)
        {
            throw new BusinessException(
                $"Kategorija \"{entitet.Oznaka}\" se ne moze obrisati jer je traze modeli vozila ({brojModela}).");
        }

        var brojPravila = await Context.PravilaKategorija.CountAsync(x => x.KategorijaDozvoleId == entitet.Id, ct);
        if (brojPravila > 0)
        {
            throw new BusinessException(
                $"Kategorija \"{entitet.Oznaka}\" se ne moze obrisati jer za nju postoje definisana pravila ({brojPravila}).");
        }

        var brojDozvola = await Context.DozvolaKategorije.CountAsync(x => x.KategorijaDozvoleId == entitet.Id, ct);
        if (brojDozvola > 0)
        {
            throw new BusinessException(
                $"Kategorija \"{entitet.Oznaka}\" se ne moze obrisati jer je upisana na vozacke dozvole ({brojDozvola}).");
        }
    }
}
