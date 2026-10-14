using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Lokacija;

namespace SunnyRides.Services.Sifrarnici;

public class PoslovnicaService
    : BaseCRUDService<PoslovnicaDto, PoslovnicaSearchObject, Poslovnica,
                      PoslovnicaInsertRequest, PoslovnicaUpdateRequest>,
      IPoslovnicaService
{
    private readonly ILokacijaKlijenta _lokacija;

    public PoslovnicaService(SunnyRidesDbContext context, ILokacijaKlijenta lokacija) : base(context)
    {
        _lokacija = lokacija;
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
        Koordinate.ProvjeriPar(request.Latituda, request.Longituda);
    }

    protected override async Task BeforeUpdateAsync(
        PoslovnicaUpdateRequest request, Poslovnica entitet, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<Grad>(request.GradId, "Grad", ct);
        Koordinate.ProvjeriPar(request.Latituda, request.Longituda);
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

        var brojUposlenika = await Context.Korisnici.CountAsync(x => x.PoslovnicaId == entitet.Id, ct);
        if (brojUposlenika > 0)
        {
            throw new BusinessException(
                $"Poslovnica \"{entitet.Naziv}\" se ne moze obrisati jer u njoj rade uposlenici ({brojUposlenika}). Prvo ih premjestite u drugu poslovnicu.");
        }
    }

    public async Task<NajblizaPoslovnicaDto?> NajblizaAsync(
        double? latituda, double? longituda, CancellationToken ct = default)
    {
        var polaziste = await _lokacija.PolazisteAsync(latituda, longituda, ct);

        if (polaziste is null)
        {
            return null;
        }

        var udaljenosti = await _lokacija.UdaljenostiPoslovnicaAsync(polaziste, ct);

        if (udaljenosti.Count == 0)
        {
            return null;
        }

        var (poslovnicaId, km) = udaljenosti
            .OrderBy(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => (x.Key, x.Value))
            .First();

        var zaokruzeno = Math.Round(km, 1);
        var poLokaciji = polaziste.PoLokaciji;
        var polazniGrad = polaziste.GradNaziv;

        return await Context.Poslovnice
            .AsNoTracking()
            .Where(x => x.Id == poslovnicaId)
            .Select(x => new NajblizaPoslovnicaDto
            {
                Id = x.Id,
                Naziv = x.Naziv,
                Adresa = x.Adresa,
                GradNaziv = x.Grad.Naziv,
                RadnoVrijeme = x.RadnoVrijeme,
                UdaljenostKm = zaokruzeno,
                PoLokaciji = poLokaciji,
                PolazniGrad = polazniGrad
            })
            .FirstOrDefaultAsync(ct);
    }
}
