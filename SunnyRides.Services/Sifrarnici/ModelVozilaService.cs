using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

public class ModelVozilaService
    : BaseCRUDService<ModelVozilaDto, ModelVozilaSearchObject, ModelVozila,
                      ModelVozilaInsertRequest, ModelVozilaUpdateRequest>,
      IModelVozilaService
{
    public ModelVozilaService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Model vozila";

    protected override string PodrazumijevaniPoredak => "Naziv";

    protected override IQueryable<ModelVozila> AddFilter(
        ModelVozilaSearchObject search, IQueryable<ModelVozila> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.Naziv))
        {
            upit = upit.Where(x => x.Naziv.Contains(search.Naziv));
        }

        if (search.MarkaId.HasValue)
        {
            upit = upit.Where(x => x.MarkaId == search.MarkaId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.TipVozilaId == search.TipVozilaId.Value);
        }

        if (search.TipGorivaId.HasValue)
        {
            upit = upit.Where(x => x.TipGorivaId == search.TipGorivaId.Value);
        }

        if (search.KategorijaDozvoleId.HasValue)
        {
            upit = upit.Where(x => x.KategorijaDozvoleId == search.KategorijaDozvoleId.Value);
        }

        if (search.KubikazaOd.HasValue)
        {
            upit = upit.Where(x => x.Kubikaza >= search.KubikazaOd.Value);
        }

        if (search.KubikazaDo.HasValue)
        {
            upit = upit.Where(x => x.Kubikaza <= search.KubikazaDo.Value);
        }

        return upit;
    }

    // Cetiri navigacije u jednom upitu. Alternativa - dohvat naziva po redu - bila
    // bi klasican N+1 slucaj koji uputstvo izricito navodi kao gresku.
    protected override IQueryable<ModelVozila> AddInclude(
        ModelVozilaSearchObject search, IQueryable<ModelVozila> upit) =>
        upit.Include(x => x.Marka)
            .Include(x => x.TipVozila)
            .Include(x => x.TipGoriva)
            .Include(x => x.KategorijaDozvole);

    protected override IQueryable<ModelVozila> AddIncludeDetalji(IQueryable<ModelVozila> upit) =>
        upit.Include(x => x.Marka)
            .Include(x => x.TipVozila)
            .Include(x => x.TipGoriva)
            .Include(x => x.KategorijaDozvole);

    protected override string PorukaZaDuplikat() =>
        "Model sa tim nazivom vec postoji za odabranu marku.";

    protected override async Task BeforeInsertAsync(
        ModelVozilaInsertRequest request, ModelVozila entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.MarkaId, request.TipVozilaId,
                                request.TipGorivaId, request.KategorijaDozvoleId, ct);
        await ObaveznoUskladjenSaKategorijomAsync(entitet, ct);
    }

    protected override async Task BeforeUpdateAsync(
        ModelVozilaUpdateRequest request, ModelVozila entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.MarkaId, request.TipVozilaId,
                                request.TipGorivaId, request.KategorijaDozvoleId, ct);
        await ObaveznoUskladjenSaKategorijomAsync(entitet, ct);
    }

    /// <summary>
    /// Kubikaza i snaga modela moraju stati u pravilo kategorije koja mu je upisana, za
    /// njegov tip vozila. Prijava trazi da se uskladjenost kategorije sa kubikazom i
    /// snagom provjerava na serveru - bez ovoga bi model od 649 ccm mogao biti upisan
    /// kao A1 i ponudjen klijentu koji ga ne smije voziti.
    /// </summary>
    private async Task ObaveznoUskladjenSaKategorijomAsync(ModelVozila model, CancellationToken ct)
    {
        var pravilo = await Context.PravilaKategorija
            .AsNoTracking()
            .Where(x => x.KategorijaDozvoleId == model.KategorijaDozvoleId && x.TipVozilaId == model.TipVozilaId)
            .Select(x => new PraviloUlaz(x.KategorijaDozvoleId, x.TipVozilaId, x.MaxKubikaza, x.MaxSnagaKw, x.MinGodine))
            .FirstOrDefaultAsync(ct);

        var oznaka = await Context.KategorijeDozvola
            .Where(x => x.Id == model.KategorijaDozvoleId)
            .Select(x => x.Oznaka)
            .FirstAsync(ct);

        var tip = await Context.TipoviVozila
            .Where(x => x.Id == model.TipVozilaId)
            .Select(x => x.Naziv)
            .FirstAsync(ct);

        var razlog = UskladjenostModela.Razlog(oznaka, tip, model.Kubikaza, model.SnagaKw, pravilo);

        if (razlog is not null)
        {
            throw new BusinessException(razlog);
        }
    }

    protected override async Task BeforeDeleteAsync(ModelVozila entitet, CancellationToken ct)
    {
        var brojVozila = await Context.Vozila.CountAsync(x => x.ModelVozilaId == entitet.Id, ct);
        if (brojVozila > 0)
        {
            throw new BusinessException(
                $"Model \"{entitet.Naziv}\" se ne moze obrisati jer u floti postoje vozila tog modela ({brojVozila}).");
        }

        var brojTarifa = await Context.Cjenovnici.CountAsync(x => x.ModelVozilaId == entitet.Id, ct);
        if (brojTarifa > 0)
        {
            throw new BusinessException(
                $"Model \"{entitet.Naziv}\" se ne moze obrisati jer za njega postoje tarife u cjenovniku ({brojTarifa}).");
        }
    }

    private async Task ProvjeriVezeAsync(
        int markaId, int tipVozilaId, int tipGorivaId, int kategorijaId, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Marka>(markaId, "Marka", ct);
        await ObaveznoPostojiAsync<TipVozila>(tipVozilaId, "Tip vozila", ct);
        await ObaveznoPostojiAsync<TipGoriva>(tipGorivaId, "Tip goriva", ct);
        await ObaveznoPostojiAsync<KategorijaDozvole>(kategorijaId, "Kategorija dozvole", ct);
    }
}
