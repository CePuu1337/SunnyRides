using Mapster;
using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Base;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Services.Notifikacije;

public class NotifikacijaService
    : BaseService<NotifikacijaDto, NotifikacijaSearchObject, Notifikacija>, INotifikacijaService
{
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IObjavljivacPoruka _objavljivac;

    public NotifikacijaService(
        SunnyRidesDbContext context,
        ICurrentUserService trenutniKorisnik,
        IObjavljivacPoruka objavljivac)
        : base(context)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _objavljivac = objavljivac;
    }

    protected override string NazivEntiteta => "Notifikacija";

    /// <summary>Najnovije na vrhu - to je jedini poredak koji u ovoj listi ima smisla.</summary>
    protected override string PodrazumijevaniPoredak => "DatumKreiranja desc";

    // --- citanje -----------------------------------------------------------

    /// <summary>
    /// Suzavanje na vlastite zapise ide u filter, a ne u kontroler.
    ///
    /// Za razliku od rezervacija, ovdje nema izuzetka za osoblje: obavjestenje je
    /// licna poruka i uposlenik nema razloga citati tudja. Zato uslov nije uslovan -
    /// vrijedi za svakoga ko pozove ovaj servis.
    /// </summary>
    protected override IQueryable<Notifikacija> AddFilter(
        NotifikacijaSearchObject search, IQueryable<Notifikacija> upit)
    {
        // Identifikator se cita prije upita, u lokalnu varijablu. Da poziv metode stoji
        // unutar izraza, EF bi ga morao rastavljati pri svakom prevodjenju upita.
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        upit = upit.Where(x => x.KorisnikId == korisnikId);

        if (search.Procitana.HasValue)
        {
            upit = upit.Where(x => x.Procitana == search.Procitana.Value);
        }

        if (search.Tip.HasValue)
        {
            upit = upit.Where(x => x.Tip == search.Tip.Value);
        }

        if (search.RezervacijaId.HasValue)
        {
            upit = upit.Where(x => x.RezervacijaId == search.RezervacijaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Tekst))
        {
            upit = upit.Where(x => x.Naslov.Contains(search.Tekst) || x.Tekst.Contains(search.Tekst));
        }

        if (search.OdDatuma.HasValue)
        {
            upit = upit.Where(x => x.DatumKreiranja >= search.OdDatuma.Value);
        }

        if (search.DoDatuma.HasValue)
        {
            upit = upit.Where(x => x.DatumKreiranja <= search.DoDatuma.Value);
        }

        return upit;
    }

    protected override IQueryable<Notifikacija> AddInclude(
        NotifikacijaSearchObject search, IQueryable<Notifikacija> upit) =>
        upit.Include(x => x.Rezervacija);

    protected override IQueryable<Notifikacija> AddIncludeDetalji(IQueryable<Notifikacija> upit) =>
        upit.Include(x => x.Rezervacija);

    /// <summary>
    /// Dohvat po identifikatoru. Bazna metoda ne zna za vlasnika, pa se provjera radi
    /// ovdje - inace bi bilo dovoljno mijenjati broj u adresi da se citaju tudja
    /// obavjestenja, a u njima stoje brojevi rezervacija i iznosi.
    /// </summary>
    public override async Task<NotifikacijaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var notifikacija = await UcitajVlastituAsync(id, ct);

        return notifikacija.Adapt<NotifikacijaDto>();
    }

    public async Task<BrojNeprocitanihDto> BrojNeprocitanihAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var broj = await Context.Notifikacije
            .CountAsync(x => x.KorisnikId == korisnikId && !x.Procitana, ct);

        return new BrojNeprocitanihDto { Broj = broj };
    }

    // --- oznacavanje -------------------------------------------------------

    public async Task<NotifikacijaDto> OznaciProcitanuAsync(int id, CancellationToken ct = default)
    {
        var notifikacija = await UcitajVlastituAsync(id, ct, zaIzmjenu: true);

        // Ponovljen poziv nije greska - aplikacija oznacava procitano pri otvaranju
        // stavke, pa se isti zahtjev lako posalje dvaput. Vraca se isto stanje.
        if (!notifikacija.Procitana)
        {
            notifikacija.Procitana = true;
            await Context.SaveChangesAsync(ct);
        }

        return notifikacija.Adapt<NotifikacijaDto>();
    }

    /// <summary>
    /// Oznacava sve odjednom, jednim upitom nad bazom umjesto ucitavanjem zapisa u
    /// memoriju. Korisnik koji dugo nije otvarao aplikaciju moze imati stotine
    /// neprocitanih, a njih nema smisla vuci sa baze samo da bi se promijenio jedan bit.
    /// </summary>
    public async Task<BrojNeprocitanihDto> OznaciSveProcitaneAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        await Context.Notifikacije
            .Where(x => x.KorisnikId == korisnikId && !x.Procitana)
            .ExecuteUpdateAsync(postavi => postavi.SetProperty(x => x.Procitana, true), ct);

        return new BrojNeprocitanihDto { Broj = 0 };
    }

    // --- upis --------------------------------------------------------------

    public async Task<NotifikacijaDto> KreirajAsync(
        int korisnikId, int? rezervacijaId, TipNotifikacije tip,
        string naslov, string tekst, CancellationToken ct = default)
    {
        var notifikacija = new Notifikacija
        {
            KorisnikId = korisnikId,
            RezervacijaId = rezervacijaId,
            Naslov = naslov,
            Tekst = tekst,
            Tip = tip,
            Procitana = false,
            DatumKreiranja = DateTime.UtcNow
        };

        Context.Notifikacije.Add(notifikacija);
        await Context.SaveChangesAsync(ct);

        // Guranje na uredjaj ide tek poslije upisa i van transakcije. Obrnuto bi
        // znacilo da aplikacija moze prikazati obavjestenje koje u bazi ne postoji -
        // recimo ako upis poslije toga padne.
        await _objavljivac.ObjaviSvimaAsync(
            Razmjene.Notifikacije, new NotifikacijaPoruka(notifikacija.Id, korisnikId), ct);

        return notifikacija.Adapt<NotifikacijaDto>();
    }

    // --- interno -----------------------------------------------------------

    private async Task<Notifikacija> UcitajVlastituAsync(
        int id, CancellationToken ct, bool zaIzmjenu = false)
    {
        var upit = Context.Notifikacije.Include(x => x.Rezervacija).AsQueryable();

        if (!zaIzmjenu)
        {
            upit = upit.AsNoTracking();
        }

        var notifikacija = await upit.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFoundException.Za(NazivEntiteta, id);

        if (notifikacija.KorisnikId != _trenutniKorisnik.ObaveznoKorisnikId())
        {
            throw new ForbiddenException("Mozete otvarati samo svoja obavjestenja.");
        }

        return notifikacija;
    }
}
