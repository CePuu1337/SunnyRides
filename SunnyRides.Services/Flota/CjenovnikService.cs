using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Flota;

/// <summary>
/// Sezonske tarife po modelu vozila.
///
/// Cjenovnik se cita pri svakoj pretrazi i pri svakom obracunu cijene, a mijenja se
/// nekoliko puta godisnje. Zato je kesiran - kroz <see cref="IMemoryCache"/> na
/// servisnom nivou, a ne kroz Dictionary polje u klasi. Razlika nije kozmeticka:
/// Dictionary bi zivio koliko i instanca servisa (jedan zahtjev, jer je servis
/// Scoped) pa ne bi nista ustedio, a da je staticki, nista ga ne bi cistilo ni
/// ogranicavalo.
/// </summary>
public class CjenovnikService
    : BaseCRUDService<CjenovnikDto, CjenovnikSearchObject, Cjenovnik,
                      CjenovnikInsertRequest, CjenovnikUpdateRequest>,
      ICjenovnikService
{
    private static readonly TimeSpan TrajanjeKesa = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _kes;

    public CjenovnikService(SunnyRidesDbContext context, IMemoryCache kes) : base(context)
    {
        _kes = kes;
    }

    protected override string NazivEntiteta => "Cjenovnik";

    protected override string PodrazumijevaniPoredak => "DatumOd";

    public async Task<CjenovnikDto?> VazeciAsync(
        int modelVozilaId, DateTime datum, CancellationToken ct = default)
    {
        var tarife = await TarifeModelaAsync(modelVozilaId, ct);

        // Preklapanje perioda je zabranjeno pri unosu, pa za jedan dan postoji
        // najvise jedna tarifa. FirstOrDefault ovdje nije izbor "bilo koja" nego
        // posljedica tog pravila.
        var vazeca = tarife.FirstOrDefault(x => x.DatumOd <= datum && x.DatumDo > datum);

        return vazeca?.Adapt<CjenovnikDto>();
    }

    protected override IQueryable<Cjenovnik> AddFilter(
        CjenovnikSearchObject search, IQueryable<Cjenovnik> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (search.ModelVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozilaId == search.ModelVozilaId.Value);
        }

        if (search.VaziNaDatum.HasValue)
        {
            var datum = search.VaziNaDatum.Value;
            upit = upit.Where(x => x.DatumOd <= datum && x.DatumDo > datum);
        }

        return upit;
    }

    protected override IQueryable<Cjenovnik> AddInclude(
        CjenovnikSearchObject search, IQueryable<Cjenovnik> upit) => SaModelom(upit);

    protected override IQueryable<Cjenovnik> AddIncludeDetalji(IQueryable<Cjenovnik> upit) =>
        SaModelom(upit);

    private static IQueryable<Cjenovnik> SaModelom(IQueryable<Cjenovnik> upit) =>
        upit.Include(x => x.ModelVozila).ThenInclude(m => m.Marka);

    protected override async Task BeforeInsertAsync(
        CjenovnikInsertRequest request, Cjenovnik entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<ModelVozila>(request.ModelVozilaId, "Model vozila", ct);
        ProvjeriPeriodIPragove(request.DatumOd, request.DatumDo,
            request.PopustPrag1, request.PopustProcenat1, request.PopustPrag2, request.PopustProcenat2);

        await ProvjeriPreklapanjeAsync(request.ModelVozilaId, request.DatumOd, request.DatumDo, null, ct);
    }

    protected override async Task BeforeUpdateAsync(
        CjenovnikUpdateRequest request, Cjenovnik entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<ModelVozila>(request.ModelVozilaId, "Model vozila", ct);
        ProvjeriPeriodIPragove(request.DatumOd, request.DatumDo,
            request.PopustPrag1, request.PopustProcenat1, request.PopustPrag2, request.PopustProcenat2);

        await ProvjeriPreklapanjeAsync(
            request.ModelVozilaId, request.DatumOd, request.DatumDo, entitet.Id, ct);
    }

    protected override Task AfterInsertAsync(
        CjenovnikInsertRequest request, Cjenovnik entitet, CancellationToken ct)
    {
        PonistiKes(entitet.ModelVozilaId);
        return Task.CompletedTask;
    }

    protected override Task AfterUpdateAsync(
        CjenovnikUpdateRequest request, Cjenovnik entitet, CancellationToken ct)
    {
        PonistiKes(entitet.ModelVozilaId);
        return Task.CompletedTask;
    }

    protected override Task BeforeDeleteAsync(Cjenovnik entitet, CancellationToken ct)
    {
        // Kes se ponistava i kad brisanje na kraju ne uspije. Hladan kes je samo
        // jedan upit vise; nesto sto je ostalo u kesu a vise ne postoji u bazi je
        // pogresna cijena.
        PonistiKes(entitet.ModelVozilaId);
        return Task.CompletedTask;
    }

    private async Task<List<Cjenovnik>> TarifeModelaAsync(int modelVozilaId, CancellationToken ct)
    {
        var kljuc = KljucKesa(modelVozilaId);

        if (_kes.TryGetValue(kljuc, out List<Cjenovnik>? izKesa) && izKesa is not null)
        {
            return izKesa;
        }

        var tarife = await Context.Cjenovnici
            .Where(x => x.ModelVozilaId == modelVozilaId)
            .OrderBy(x => x.DatumOd)
            .AsNoTracking()
            .ToListAsync(ct);

        // Kes ima i rok trajanja, ne samo rucno ponistavanje. Rok je mreza za slucaj
        // da se podatak promijeni mimo ovog servisa - na primjer migracijom ili
        // rucnim upitom u bazi.
        _kes.Set(kljuc, tarife, TrajanjeKesa);

        return tarife;
    }

    private void PonistiKes(int modelVozilaId) => _kes.Remove(KljucKesa(modelVozilaId));

    private static string KljucKesa(int modelVozilaId) => $"cjenovnik:model:{modelVozilaId}";

    private static void ProvjeriPeriodIPragove(
        DateTime od, DateTime doDatuma, int prag1, decimal procenat1, int prag2, decimal procenat2)
    {
        if (doDatuma <= od)
        {
            throw new BusinessException("Kraj perioda mora biti poslije pocetka.");
        }

        if (prag2 <= prag1)
        {
            throw new BusinessException("Drugi prag popusta mora biti veci od prvog.");
        }

        // Duzi najam ne smije biti procentualno manje nagradjen od kraceg - inace bi
        // klijentu bilo isplativije rezervisati krace, sto je suprotno svrsi popusta.
        if (procenat2 < procenat1)
        {
            throw new BusinessException(
                "Popust na duzem pragu ne smije biti manji od popusta na kracem.");
        }
    }

    /// <summary>
    /// Dvije tarife za isti model ne smiju vaziti istovremeno. Da smiju, obracun bi
    /// morao birati izmedju dva mnozioca, a taj izbor nigdje nije definisan - cijena
    /// bi zavisila od redoslijeda zapisa u bazi.
    /// </summary>
    private async Task ProvjeriPreklapanjeAsync(
        int modelVozilaId, DateTime od, DateTime doDatuma, int? ignorisiId, CancellationToken ct)
    {
        var upit = Context.Cjenovnici
            .Where(x => x.ModelVozilaId == modelVozilaId)
            .Where(x => x.DatumOd < doDatuma && x.DatumDo > od);

        if (ignorisiId.HasValue)
        {
            upit = upit.Where(x => x.Id != ignorisiId.Value);
        }

        var postojeca = await upit
            .Select(x => new { x.Naziv, x.DatumOd, x.DatumDo })
            .FirstOrDefaultAsync(ct);

        if (postojeca is not null)
        {
            throw new BusinessException(
                $"Period se preklapa sa tarifom \"{postojeca.Naziv}\" " +
                $"({postojeca.DatumOd:dd.MM.yyyy.} - {postojeca.DatumDo:dd.MM.yyyy.}).");
        }
    }
}
