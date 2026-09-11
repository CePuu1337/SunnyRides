using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Sifrarnici;

/// <summary>
/// Pravila o tome koja kategorija dozvole pokriva koja vozila. Ovo je podatak u
/// tabeli, a ne "if" grane u kodu - kad se propis promijeni, ispravka je unos
/// zapisa kroz ovaj sifrarnik, bez ijedne izmjene koda.
/// </summary>
public class PravilaKategorijeService
    : BaseCRUDService<PravilaKategorijeDto, PravilaKategorijeSearchObject, PravilaKategorije,
                      PravilaKategorijeInsertRequest, PravilaKategorijeUpdateRequest>,
      IPravilaKategorijeService
{
    public PravilaKategorijeService(SunnyRidesDbContext context) : base(context)
    {
    }

    protected override string NazivEntiteta => "Pravilo kategorije";

    protected override IQueryable<PravilaKategorije> AddFilter(
        PravilaKategorijeSearchObject search, IQueryable<PravilaKategorije> upit)
    {
        if (search.KategorijaDozvoleId.HasValue)
        {
            upit = upit.Where(x => x.KategorijaDozvoleId == search.KategorijaDozvoleId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.TipVozilaId == search.TipVozilaId.Value);
        }

        return upit;
    }

    protected override IQueryable<PravilaKategorije> AddInclude(
        PravilaKategorijeSearchObject search, IQueryable<PravilaKategorije> upit) =>
        upit.Include(x => x.KategorijaDozvole).Include(x => x.TipVozila);

    protected override IQueryable<PravilaKategorije> AddIncludeDetalji(
        IQueryable<PravilaKategorije> upit) =>
        upit.Include(x => x.KategorijaDozvole).Include(x => x.TipVozila);

    protected override string PorukaZaDuplikat() =>
        "Pravilo za tu kombinaciju kategorije i tipa vozila vec postoji.";

    protected override async Task BeforeInsertAsync(
        PravilaKategorijeInsertRequest request, PravilaKategorije entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.KategorijaDozvoleId, request.TipVozilaId, ct);
    }

    protected override async Task BeforeUpdateAsync(
        PravilaKategorijeUpdateRequest request, PravilaKategorije entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.KategorijaDozvoleId, request.TipVozilaId, ct);
    }

    private async Task ProvjeriVezeAsync(int kategorijaId, int tipVozilaId, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<KategorijaDozvole>(kategorijaId, "Kategorija dozvole", ct);
        await ObaveznoPostojiAsync<TipVozila>(tipVozilaId, "Tip vozila", ct);
    }
}
