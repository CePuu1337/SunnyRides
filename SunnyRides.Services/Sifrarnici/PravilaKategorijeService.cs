using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Sifrarnici;

/// <summary>
/// Pravila o tome koja kategorija dozvole pokriva koja vozila. Ovo je podatak u
/// tabeli, a ne "if" grane u kodu - kad se propis promijeni, ispravka je unos
/// zapisa kroz ovaj sifrarnik, bez ijedne izmjene koda.
///
/// Pravila se kesiraju u <see cref="DozvolaService"/>, jer se citaju pri svakoj
/// pretrazi. Svaka izmjena ovdje taj kes brise, inace bi nova pravila u pretrazi,
/// verifikaciji i rezervaciji vazila tek kad kes istekne.
/// </summary>
public class PravilaKategorijeService
    : BaseCRUDService<PravilaKategorijeDto, PravilaKategorijeSearchObject, PravilaKategorije,
                      PravilaKategorijeInsertRequest, PravilaKategorijeUpdateRequest>,
      IPravilaKategorijeService
{
    private readonly IMemoryCache _kes;

    public PravilaKategorijeService(SunnyRidesDbContext context, IMemoryCache kes) : base(context)
    {
        _kes = kes;
    }

    /// <summary>Isto za dodavanje, izmjenu i brisanje - poslije potvrde u bazi.</summary>
    protected override void NakonPotvrde(PravilaKategorije entitet) =>
        KesPravilaKategorija.Ponisti(_kes);

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

        // Zahtjev je vec prepisan preko entiteta, pa su kategorija i tip prije izmjene
        // jos samo u originalnim vrijednostima koje EF pamti.
        var staraKategorija = Context.Entry(entitet).Property(x => x.KategorijaDozvoleId).OriginalValue;
        var stariTip = Context.Entry(entitet).Property(x => x.TipVozilaId).OriginalValue;

        // Pravilo prebaceno na drugu kategoriju ili tip vise ne pokriva modele za koje je
        // vazilo do sada - za njih ono nestaje.
        if (staraKategorija != entitet.KategorijaDozvoleId || stariTip != entitet.TipVozilaId)
        {
            await ObaveznoBezModelaIzvanPravilaAsync(staraKategorija, stariTip, null, ct);
        }

        await ObaveznoBezModelaIzvanPravilaAsync(entitet.KategorijaDozvoleId, entitet.TipVozilaId, entitet, ct);
    }

    protected override async Task BeforeDeleteAsync(PravilaKategorije entitet, CancellationToken ct)
    {
        await ObaveznoBezModelaIzvanPravilaAsync(entitet.KategorijaDozvoleId, entitet.TipVozilaId, null, ct);
    }

    /// <summary>
    /// Izmjena ili brisanje pravila ne smije ostaviti postojece modele izvan njega.
    ///
    /// Model nosi kategoriju koja mu treba, a pretraga je cita sa modela. Kad bi se A1
    /// spustilo na 50 ccm, skuteri od 125 ccm bi i dalje bili A1 i nudili bi se
    /// klijentima kojima novo pravilo to vise ne dozvoljava. Zato se takva izmjena
    /// odbija, uz spisak modela kojima prvo treba promijeniti kategoriju.
    ///
    /// <paramref name="novo"/> je null kad za ovu kategoriju i tip pravilo nestaje
    /// (brisanje, ili pravilo prebaceno drugdje) - tada su izvan pravila svi takvi modeli.
    /// </summary>
    private async Task ObaveznoBezModelaIzvanPravilaAsync(
        int kategorijaId, int tipVozilaId, PravilaKategorije? novo, CancellationToken ct)
    {
        var upit = Context.ModeliVozila
            .AsNoTracking()
            .Where(x => x.KategorijaDozvoleId == kategorijaId && x.TipVozilaId == tipVozilaId);

        if (novo is not null)
        {
            var maxKubikaza = novo.MaxKubikaza;
            var maxSnaga = novo.MaxSnagaKw;

            // Isto pravilo kao UskladjenostModela.PrelaziKubikazu i PrelaziSnagu, kao
            // uslov na bazi: prazna granica ne ogranicava nista.
            upit = upit.Where(x => (maxKubikaza != null && x.Kubikaza > maxKubikaza)
                                   || (maxSnaga != null && x.SnagaKw > maxSnaga));
        }

        const int NajviseUPoruci = 5;

        var ukupno = await upit.CountAsync(ct);

        if (ukupno == 0)
        {
            return;
        }

        var modeli = await upit
            .OrderBy(x => x.Marka.Naziv).ThenBy(x => x.Naziv)
            .Take(NajviseUPoruci)
            .Select(x => x.Marka.Naziv + " " + x.Naziv + " (" + x.Kubikaza + " ccm)")
            .ToListAsync(ct);

        var ostali = ukupno > modeli.Count ? $" i jos {ukupno - modeli.Count}" : string.Empty;

        throw new BusinessException(novo is null
            ? $"Bez ovog pravila modeli {string.Join(", ", modeli)}{ostali} ne bi bili pokriveni " +
              "svojom kategorijom. Prvo im promijenite kategoriju u sifarniku Modeli vozila."
            : $"Ovom pravilu ne bi odgovarali modeli {string.Join(", ", modeli)}{ostali}. " +
              "Prvo im promijenite kategoriju u sifarniku Modeli vozila.");
    }

    private async Task ProvjeriVezeAsync(int kategorijaId, int tipVozilaId, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<KategorijaDozvole>(kategorijaId, "Kategorija dozvole", ct);
        await ObaveznoPostojiAsync<TipVozila>(tipVozilaId, "Tip vozila", ct);
    }
}
