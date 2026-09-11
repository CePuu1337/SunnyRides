using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Database;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Base;

/// <summary>
/// Nasljedjuje citanje i dodaje upis, izmjenu i brisanje.
///
/// Hookovi <see cref="BeforeInsertAsync"/>, <see cref="AfterInsertAsync"/>,
/// <see cref="BeforeUpdateAsync"/> i <see cref="BeforeDeleteAsync"/> su mjesta gdje
/// konkretni servisi dodaju poslovna pravila, bez prepisivanja cijele metode.
/// </summary>
public abstract class BaseCRUDService<TModel, TSearch, TEntity, TInsert, TUpdate>
    : BaseService<TModel, TSearch, TEntity>, ICRUDService<TModel, TSearch, TInsert, TUpdate>
    where TSearch : BaseSearchObject
    where TEntity : class, new()
{
    protected BaseCRUDService(SunnyRidesDbContext context) : base(context)
    {
    }

    public virtual async Task<TModel> InsertAsync(TInsert request, CancellationToken ct = default)
    {
        return await UTransakcijiAsync(async () =>
        {
            var entitet = new TEntity();
            request.Adapt(entitet);

            await BeforeInsertAsync(request, entitet, ct);

            Context.Set<TEntity>().Add(entitet);
            await SacuvajAsync(ct);

            await AfterInsertAsync(request, entitet, ct);

            return entitet.Adapt<TModel>();
        }, ct);
    }

    public virtual async Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken ct = default)
    {
        return await UTransakcijiAsync(async () =>
        {
            var entitet = await Context.Set<TEntity>().FirstOrDefaultAsync(NadjiPoId(id), ct)
                          ?? throw NotFoundException.Za(NazivEntiteta, id);

            request.Adapt(entitet);

            await BeforeUpdateAsync(request, entitet, ct);
            await SacuvajAsync(ct);
            await AfterUpdateAsync(request, entitet, ct);

            return entitet.Adapt<TModel>();
        }, ct);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await UTransakcijiAsync<object?>(async () =>
        {
            var entitet = await Context.Set<TEntity>().FirstOrDefaultAsync(NadjiPoId(id), ct)
                          ?? throw NotFoundException.Za(NazivEntiteta, id);

            // Ovdje konkretni servis odbija brisanje zapisa koji se koristi, i to
            // porukom koju korisnik moze razumjeti - a ne EF izuzetkom.
            await BeforeDeleteAsync(entitet, ct);

            Context.Set<TEntity>().Remove(entitet);

            try
            {
                await Context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                throw new BusinessException(
                    $"{NazivEntiteta} se ne moze obrisati jer postoje zapisi koji ga koriste.", ex);
            }

            return null;
        }, ct);
    }

    // --- hookovi ----------------------------------------------------------

    protected virtual Task BeforeInsertAsync(TInsert request, TEntity entitet, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task AfterInsertAsync(TInsert request, TEntity entitet, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task BeforeUpdateAsync(TUpdate request, TEntity entitet, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task AfterUpdateAsync(TUpdate request, TEntity entitet, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task BeforeDeleteAsync(TEntity entitet, CancellationToken ct)
        => Task.CompletedTask;

    // --- interno ----------------------------------------------------------

    /// <summary>
    /// Obavija operaciju eksplicitnom transakcijom. Hookovi smiju pozvati
    /// SaveChangesAsync, pa operacija moze imati vise upisa - a vise upisa u jednoj
    /// operaciji mora biti u transakciji.
    ///
    /// Ako je pozivalac vec otvorio transakciju, ova metoda se ne mijesa - transakcijom
    /// upravlja onaj ko ju je otvorio.
    /// </summary>
    protected async Task<T> UTransakcijiAsync<T>(Func<Task<T>> posao, CancellationToken ct)
    {
        if (Context.Database.CurrentTransaction is not null)
        {
            return await posao();
        }

        await using var transakcija = await Context.Database.BeginTransactionAsync(ct);
        var rezultat = await posao();
        await transakcija.CommitAsync(ct);
        return rezultat;
    }
}
