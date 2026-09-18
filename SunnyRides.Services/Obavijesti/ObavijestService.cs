using Microsoft.EntityFrameworkCore;
using SunnyRides.Model;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Obavijesti;

public class ObavijestService
    : BaseCRUDService<ObavijestDto, ObavijestSearchObject, Obavijest,
                      ObavijestInsertRequest, ObavijestUpdateRequest>,
      IObavijestService
{
    private const string Podfolder = "obavijesti";

    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IPohranaSlika _pohrana;

    /// <summary>Kad zahtjev dolazi od klijenta, lista se suzava na objavljene obavijesti.</summary>
    private bool _samoObjavljene;

    public ObavijestService(
        SunnyRidesDbContext context, ICurrentUserService trenutniKorisnik, IPohranaSlika pohrana)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _pohrana = pohrana;
    }

    protected override string NazivEntiteta => "Obavijest";

    /// <summary>Najnovije na vrhu - to je jedini poredak koji na pocetnom ekranu ima smisla.</summary>
    protected override string PodrazumijevaniPoredak => "DatumObjave desc";

    // --- citanje -----------------------------------------------------------

    public override async Task<PagedResult<ObavijestDto>> GetAsync(
        ObavijestSearchObject search, CancellationToken ct = default)
    {
        _samoObjavljene = !JeOsoblje();

        return await base.GetAsync(search, ct);
    }

    public override async Task<ObavijestDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var obavijest = await base.GetByIdAsync(id, ct);

        // Neobjavljena obavijest za klijenta ne postoji. Namjerno 404, a ne 403: da
        // odgovor ne odaje da nesto postoji i ceka objavu.
        if (!JeOsoblje() && (!obavijest.Aktivna || obavijest.DatumObjave > DateTime.UtcNow))
        {
            throw NotFoundException.Za(NazivEntiteta, id);
        }

        return obavijest;
    }

    protected override IQueryable<Obavijest> AddFilter(
        ObavijestSearchObject search, IQueryable<Obavijest> upit)
    {
        if (_samoObjavljene)
        {
            var sada = DateTime.UtcNow;
            upit = upit.Where(x => x.Aktivna && x.DatumObjave <= sada);
        }
        else if (search.Aktivna.HasValue)
        {
            upit = upit.Where(x => x.Aktivna == search.Aktivna.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Tekst))
        {
            upit = upit.Where(x => x.Naslov.Contains(search.Tekst) || x.Tekst.Contains(search.Tekst));
        }

        if (search.OdDatuma.HasValue)
        {
            upit = upit.Where(x => x.DatumObjave >= search.OdDatuma.Value);
        }

        if (search.DoDatuma.HasValue)
        {
            upit = upit.Where(x => x.DatumObjave <= search.DoDatuma.Value);
        }

        return upit;
    }

    // --- upis --------------------------------------------------------------

    protected override Task BeforeInsertAsync(
        ObavijestInsertRequest request, Obavijest entitet, CancellationToken ct)
    {
        entitet.Naslov = request.Naslov.Trim();
        entitet.Tekst = request.Tekst.Trim();

        // Bez datuma objava vazi odmah. Datum u buducnosti je zakazana objava.
        entitet.DatumObjave = request.DatumObjave ?? DateTime.UtcNow;
        entitet.PutanjaSlike = null;

        return Task.CompletedTask;
    }

    protected override Task BeforeUpdateAsync(
        ObavijestUpdateRequest request, Obavijest entitet, CancellationToken ct)
    {
        entitet.Naslov = request.Naslov.Trim();
        entitet.Tekst = request.Tekst.Trim();

        if (request.DatumObjave.HasValue)
        {
            entitet.DatumObjave = request.DatumObjave.Value;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Uz obavijest se brise i njena slika. Zapis u bazi nestaje u transakciji, a fajl
    /// na disku bi bez ovoga ostao zauvijek - bez ijednog zapisa koji na njega pokazuje.
    /// </summary>
    protected override Task BeforeDeleteAsync(Obavijest entitet, CancellationToken ct)
    {
        ObrisiSliku(entitet.PutanjaSlike);

        return Task.CompletedTask;
    }

    // --- slika -------------------------------------------------------------

    public async Task<ObavijestDto> PostaviSlikuAsync(
        int id, Stream sadrzaj, long duzinaBajta, CancellationToken ct = default)
    {
        var obavijest = await Context.Obavijesti.FirstOrDefaultAsync(x => x.Id == id, ct)
                        ?? throw NotFoundException.Za(NazivEntiteta, id);

        // Sadrzaj se validira po magic bytes unutar pohrane, ne po ekstenziji naziva.
        var sacuvana = await _pohrana.SacuvajJavnoAsync(sadrzaj, duzinaBajta, Podfolder, ct);

        var stara = obavijest.PutanjaSlike;

        obavijest.PutanjaSlike = sacuvana.Putanja;
        await Context.SaveChangesAsync(ct);

        // Stara slika se brise tek kad je nova upisana u bazu. Obrnutim redoslijedom bi
        // pad upisa ostavio obavijest sa putanjom do fajla kojeg vise nema.
        ObrisiSliku(stara);

        return await base.GetByIdAsync(id, ct);
    }

    public async Task<ObavijestDto> UkloniSlikuAsync(int id, CancellationToken ct = default)
    {
        var obavijest = await Context.Obavijesti.FirstOrDefaultAsync(x => x.Id == id, ct)
                        ?? throw NotFoundException.Za(NazivEntiteta, id);

        if (obavijest.PutanjaSlike is null)
        {
            throw new BusinessException("Ova obavijest nema sliku.");
        }

        var stara = obavijest.PutanjaSlike;

        obavijest.PutanjaSlike = null;
        await Context.SaveChangesAsync(ct);

        ObrisiSliku(stara);

        return await base.GetByIdAsync(id, ct);
    }

    private void ObrisiSliku(string? putanja)
    {
        if (putanja is null)
        {
            return;
        }

        _pohrana.ObrisiJavno(putanja, PutanjeSlika.Thumbnail(putanja));
    }

    private bool JeOsoblje() =>
        _trenutniKorisnik.JeUUlozi(Uloge.Administrator) || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);
}
