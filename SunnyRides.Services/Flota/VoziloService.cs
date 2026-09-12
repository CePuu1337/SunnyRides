using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Fajlovi;

namespace SunnyRides.Services.Flota;

public class VoziloService
    : BaseCRUDService<VoziloDto, VoziloSearchObject, Vozilo, VoziloInsertRequest, VoziloUpdateRequest>,
      IVoziloService
{
    private readonly IPohranaSlika _pohrana;

    public VoziloService(SunnyRidesDbContext context, IPohranaSlika pohrana) : base(context)
    {
        _pohrana = pohrana;
    }

    protected override string NazivEntiteta => "Vozilo";

    protected override string PodrazumijevaniPoredak => "RegistarskaOznaka";

    protected override IQueryable<Vozilo> AddFilter(VoziloSearchObject search, IQueryable<Vozilo> upit)
    {
        if (!string.IsNullOrWhiteSpace(search.ModelNaziv))
        {
            upit = upit.Where(x => x.ModelVozila.Naziv.Contains(search.ModelNaziv));
        }

        if (!string.IsNullOrWhiteSpace(search.RegistarskaOznaka))
        {
            upit = upit.Where(x => x.RegistarskaOznaka.Contains(search.RegistarskaOznaka));
        }

        if (search.ModelVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozilaId == search.ModelVozilaId.Value);
        }

        if (search.MarkaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.MarkaId == search.MarkaId.Value);
        }

        if (search.TipVozilaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipVozilaId == search.TipVozilaId.Value);
        }

        if (search.TipGorivaId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.TipGorivaId == search.TipGorivaId.Value);
        }

        // Filtriranje po kategoriji dozvole je isti uslov koji ce faza 10 koristiti
        // za skrivanje vozila koja klijent ne smije voziti.
        if (search.KategorijaDozvoleId.HasValue)
        {
            upit = upit.Where(x => x.ModelVozila.KategorijaDozvoleId == search.KategorijaDozvoleId.Value);
        }

        if (search.PoslovnicaId.HasValue)
        {
            upit = upit.Where(x => x.PoslovnicaId == search.PoslovnicaId.Value);
        }

        if (search.GradId.HasValue)
        {
            upit = upit.Where(x => x.Poslovnica.GradId == search.GradId.Value);
        }

        if (search.CijenaOd.HasValue)
        {
            upit = upit.Where(x => x.DnevnaTarifa >= search.CijenaOd.Value);
        }

        if (search.CijenaDo.HasValue)
        {
            upit = upit.Where(x => x.DnevnaTarifa <= search.CijenaDo.Value);
        }

        if (search.Aktivno.HasValue)
        {
            upit = upit.Where(x => x.Aktivno == search.Aktivno.Value);
        }

        return upit;
    }

    // Lista vuce samo glavnu sliku, i to filtriranim Include-om - ostale fotografije
    // ostaju na serveru dok ih neko stvarno ne zatrazi kroz /api/vozila/{id}/slike.
    protected override IQueryable<Vozilo> AddInclude(VoziloSearchObject search, IQueryable<Vozilo> upit) =>
        SaPovezanim(upit);

    protected override IQueryable<Vozilo> AddIncludeDetalji(IQueryable<Vozilo> upit) =>
        SaPovezanim(upit);

    private static IQueryable<Vozilo> SaPovezanim(IQueryable<Vozilo> upit) =>
        upit.Include(x => x.ModelVozila).ThenInclude(m => m.Marka)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipVozila)
            .Include(x => x.ModelVozila).ThenInclude(m => m.TipGoriva)
            .Include(x => x.ModelVozila).ThenInclude(m => m.KategorijaDozvole)
            .Include(x => x.Poslovnica).ThenInclude(p => p.Grad)
            .Include(x => x.Slike.Where(s => s.JeGlavna));

    protected override string PorukaZaDuplikat() =>
        "Vozilo sa tom registarskom oznakom vec postoji.";

    protected override async Task BeforeInsertAsync(
        VoziloInsertRequest request, Vozilo entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.ModelVozilaId, request.PoslovnicaId, ct);
        ProvjeriGodinu(request.GodinaProizvodnje);
        ProvjeriTarife(request.SatnaTarifa, request.DnevnaTarifa);

        entitet.RegistarskaOznaka = NormalizujOznaku(request.RegistarskaOznaka);
        entitet.Aktivno = true;
        entitet.DatumKreiranja = DateTime.UtcNow;
    }

    protected override async Task BeforeUpdateAsync(
        VoziloUpdateRequest request, Vozilo entitet, CancellationToken ct)
    {
        await ProvjeriVezeAsync(request.ModelVozilaId, request.PoslovnicaId, ct);
        ProvjeriGodinu(request.GodinaProizvodnje);
        ProvjeriTarife(request.SatnaTarifa, request.DnevnaTarifa);

        entitet.RegistarskaOznaka = NormalizujOznaku(request.RegistarskaOznaka);
    }

    /// <summary>
    /// Vozilo koje ima rezervacije se ne brise. Historija najmova mora ostati citava
    /// radi izvjestaja, a brisanje vozila povuklo bi za sobom dio te historije ili bi
    /// puklo na stranom kljucu. Umjesto toga se deaktivira - nestane iz pretrage i
    /// kalendara flote, a proslost ostaje netaknuta.
    /// </summary>
    protected override async Task BeforeDeleteAsync(Vozilo entitet, CancellationToken ct)
    {
        var brojRezervacija = await Context.Rezervacije.CountAsync(x => x.VoziloId == entitet.Id, ct);
        if (brojRezervacija > 0)
        {
            throw new BusinessException(
                $"Vozilo \"{entitet.RegistarskaOznaka}\" se ne moze obrisati jer postoje rezervacije " +
                $"({brojRezervacija}). Deaktivirajte ga umjesto brisanja.");
        }

        var brojBlokada = await Context.BlokadeVozila.CountAsync(x => x.VoziloId == entitet.Id, ct);
        if (brojBlokada > 0)
        {
            throw new BusinessException(
                $"Vozilo \"{entitet.RegistarskaOznaka}\" se ne moze obrisati jer za njega postoje " +
                $"evidentirane blokade ({brojBlokada}).");
        }

        // Slike se u bazi brisu kaskadno, ali fajlovi na disku ne - njih uklanjamo
        // ovdje, dok se jos zna gdje su.
        // Iz baze se povlace samo dvije kolone, pa se par putanja spaja u listu
        // ovdje. SelectMany sa novim nizom SQL ne zna prevesti - ovo bi puklo u
        // trenutku brisanja, a ne pri prevodjenju.
        var slike = await Context.SlikeVozila
            .Where(x => x.VoziloId == entitet.Id)
            .Select(x => new { x.Putanja, x.PutanjaThumbnail })
            .ToListAsync(ct);

        _pohrana.ObrisiJavno(
            slike.SelectMany(x => new[] { x.Putanja, x.PutanjaThumbnail }).ToArray());
    }

    private async Task ProvjeriVezeAsync(int modelVozilaId, int poslovnicaId, CancellationToken ct)
    {
        await ObaveznoPostojiAsync<ModelVozila>(modelVozilaId, "Model vozila", ct);
        await ObaveznoPostojiAsync<Poslovnica>(poslovnicaId, "Poslovnica", ct);
    }

    /// <summary>
    /// Gornja granica je tekuca godina plus jedna, jer se novi modeli prodaju kao
    /// sljedece godiste. Anotacija to ne moze izraziti - ona prima konstantu, a ova
    /// granica se pomjera svake godine.
    /// </summary>
    private static void ProvjeriGodinu(int godina)
    {
        var najvise = DateTime.UtcNow.Year + 1;

        if (godina > najvise)
        {
            throw new BusinessException($"Godina proizvodnje ne moze biti poslije {najvise}.");
        }
    }

    /// <summary>
    /// Dnevna tarifa mora biti veca od satne. Da nije, najam od 24 sata bio bi jeftiniji
    /// od najma od jednog sata, a PricingService bi uredno izracunao besmislicu.
    /// </summary>
    private static void ProvjeriTarife(decimal satna, decimal dnevna)
    {
        if (dnevna <= satna)
        {
            throw new BusinessException("Dnevna tarifa mora biti veca od satne.");
        }
    }

    private static string NormalizujOznaku(string oznaka) =>
        oznaka.Trim().ToUpperInvariant();
}
