using System.Linq.Expressions;
using System.Reflection;
using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Database;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Base;

/// <summary>
/// Genericko citanje: paginacija, filtriranje i sortiranje na jednom mjestu.
/// Konkretni servisi nadjacavaju <see cref="AddFilter"/> i <see cref="AddInclude"/>,
/// a sve ostalo nasljedjuju.
/// </summary>
public abstract class BaseService<TModel, TSearch, TEntity> : IService<TModel, TSearch>
    where TSearch : BaseSearchObject
    where TEntity : class
{
    /// <summary>
    /// Gornja granica velicine stranice. Nametnuta je ovdje, na jednom mjestu, da se
    /// ne mora pamtiti u svakom servisu. Endpoint bez ovog ogranicenja je prema
    /// uputstvu greska - moze nepotrebno opteretiti server.
    /// </summary>
    public const int MaksimalnaVelicinaStranice = 100;

    public const int PodrazumijevanaVelicinaStranice = 20;

    protected readonly SunnyRidesDbContext Context;

    protected BaseService(SunnyRidesDbContext context)
    {
        Context = context;
    }

    /// <summary>Naziv entiteta u porukama o gresci, npr. "Drzava sa identifikatorom 5 ne postoji."</summary>
    protected virtual string NazivEntiteta => typeof(TEntity).Name;

    /// <summary>Poredak kad klijent ne zatrazi svoj. Stabilan poredak je uslov da paginacija ima smisla.</summary>
    protected virtual string PodrazumijevaniPoredak => "Id";

    public virtual async Task<PagedResult<TModel>> GetAsync(TSearch search, CancellationToken ct = default)
    {
        var upit = Context.Set<TEntity>().AsQueryable();

        upit = AddFilter(search, upit);

        // Broj se racuna prije paginacije i prije Include-a - brojanju povezani
        // zapisi ne trebaju, a nosili bi nepotrebne JOIN-ove.
        int? ukupno = null;
        if (search.IncludeTotalCount)
        {
            ukupno = await upit.CountAsync(ct);
        }

        upit = AddInclude(search, upit);
        upit = AddSort(search, upit);

        var stranica = Math.Max(search.Page ?? 0, 0);
        var velicina = Math.Clamp(
            search.PageSize ?? PodrazumijevanaVelicinaStranice, 1, MaksimalnaVelicinaStranice);

        var zapisi = await upit
            .Skip(stranica * velicina)
            .Take(velicina)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResult<TModel>
        {
            Items = zapisi.Adapt<List<TModel>>(),
            TotalCount = ukupno
        };
    }

    public virtual async Task<TModel> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var upit = AddIncludeDetalji(Context.Set<TEntity>().AsQueryable());
        var entitet = await upit.AsNoTracking().FirstOrDefaultAsync(NadjiPoId(id), ct);

        if (entitet is null)
        {
            throw NotFoundException.Za(NazivEntiteta, id);
        }

        return entitet.Adapt<TModel>();
    }

    // --- tacke u koje konkretni servisi ulaze -----------------------------

    protected virtual IQueryable<TEntity> AddFilter(TSearch search, IQueryable<TEntity> upit) => upit;

    /// <summary>Ucitavanje povezanih zapisa za listu. Lista namjerno vuce manje od detalja.</summary>
    protected virtual IQueryable<TEntity> AddInclude(TSearch search, IQueryable<TEntity> upit) => upit;

    /// <summary>
    /// Ucitavanje povezanih zapisa za detaljni prikaz. Odvojeno je od liste jer
    /// detalj smije vuci vise - na primjer, cijelu galeriju slika umjesto samo thumbnaila.
    /// </summary>
    protected virtual IQueryable<TEntity> AddIncludeDetalji(IQueryable<TEntity> upit) => upit;

    // --- interno ----------------------------------------------------------

    /// <summary>
    /// Sortiranje po nazivu svojstva koje stize iz zahtjeva. Prihvata se oblik
    /// "Naziv" i "Naziv desc". Naziv se provjerava prema stvarnim svojstvima
    /// entiteta - vrijednost koja ne odgovara nijednom svojstvu se ignorise,
    /// pa kroz ovaj parametar nije moguce ubaciti proizvoljan izraz u upit.
    /// </summary>
    protected virtual IQueryable<TEntity> AddSort(TSearch search, IQueryable<TEntity> upit)
    {
        var trazeno = string.IsNullOrWhiteSpace(search.OrderBy)
            ? PodrazumijevaniPoredak
            : search.OrderBy;

        var dijelovi = trazeno.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nazivSvojstva = dijelovi[0];
        var opadajuce = dijelovi.Length > 1
                        && dijelovi[1].StartsWith("desc", StringComparison.OrdinalIgnoreCase);

        var svojstvo = typeof(TEntity).GetProperty(nazivSvojstva,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (svojstvo is null)
        {
            svojstvo = typeof(TEntity).GetProperty(PodrazumijevaniPoredak,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (svojstvo is null)
            {
                return upit;
            }

            opadajuce = false;
        }

        var parametar = Expression.Parameter(typeof(TEntity), "x");
        var pristup = Expression.MakeMemberAccess(parametar, svojstvo);
        var lambda = Expression.Lambda(pristup, parametar);

        var poziv = Expression.Call(
            typeof(Queryable),
            opadajuce ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy),
            new[] { typeof(TEntity), svojstvo.PropertyType },
            upit.Expression,
            Expression.Quote(lambda));

        return upit.Provider.CreateQuery<TEntity>(poziv);
    }

    /// <summary>Uslov po primarnom kljucu, gradjen izrazom da ostane na bazi.</summary>
    protected static Expression<Func<TEntity, bool>> NadjiPoId(int id) => UslovPoId<TEntity>(id);

    /// <summary>Isti uslov, ali za bilo koji entitet - treba pri provjeri stranih kljuceva.</summary>
    protected static Expression<Func<T, bool>> UslovPoId<T>(int id)
    {
        var parametar = Expression.Parameter(typeof(T), "x");
        var svojstvo = Expression.Property(parametar, "Id");
        var poredjenje = Expression.Equal(svojstvo, Expression.Constant(id));
        return Expression.Lambda<Func<T, bool>>(poredjenje, parametar);
    }

    /// <summary>
    /// Provjerava da zapis na koji zahtjev pokazuje stvarno postoji.
    ///
    /// Bez ove provjere strani kljuc koji ne postoji prolazi kroz servis i puca tek
    /// u bazi, pa klijent dobije 500 sa porukom o krsenju FK ogranicenja. Ovako
    /// dobije 400 i recenicu koja mu kaze sta da popravi.
    ///
    /// Namjerno je BusinessException a ne NotFoundException: nije trazeni resurs taj
    /// koji ne postoji, nego je zahtjev pogresan - 404 bi znacio da endpoint ne
    /// postoji, a on postoji.
    /// </summary>
    protected async Task ObaveznoPostojiAsync<TStrani>(
        int id, string naziv, CancellationToken ct) where TStrani : class
    {
        var postoji = await Context.Set<TStrani>().AnyAsync(UslovPoId<TStrani>(id), ct);

        if (!postoji)
        {
            throw new BusinessException($"{naziv} sa identifikatorom {id} ne postoji.");
        }
    }

    /// <summary>
    /// Cuvanje sa prevodjenjem krsenja jedinstvenog indeksa u razumljivu poruku.
    /// Bez ovoga bi klijent na duplikat dobio 500 sa porukom iz SQL Servera.
    /// </summary>
    protected async Task SacuvajAsync(CancellationToken ct)
    {
        try
        {
            await Context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (JeKrsenjeJedinstvenosti(ex))
        {
            throw new BusinessException(PorukaZaDuplikat(), ex);
        }
    }

    /// <summary>SQL Server: 2601 je jedinstveni indeks, 2627 je jedinstveno ogranicenje.</summary>
    protected static bool JeKrsenjeJedinstvenosti(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql
        && sql.Number is 2601 or 2627;

    /// <summary>Konkretni servisi ovo nadjacavaju da poruka imenuje bas polje koje se ponavlja.</summary>
    protected virtual string PorukaZaDuplikat() =>
        "Zapis sa unesenim vrijednostima vec postoji.";
}
