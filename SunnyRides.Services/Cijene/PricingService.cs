using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Database;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Flota;

namespace SunnyRides.Services.Cijene;

/// <summary>
/// Ucitava sve sto obracunu treba i prepusta racunanje klasi <see cref="ObracunCijene"/>.
///
/// Podjela je namjerna: ovdje su upiti, tamo je aritmetika. Aritmetika se time moze
/// testirati bez baze, a ovaj sloj ostaje dovoljno jednostavan da se vidi sta cita.
/// </summary>
public class PricingService : IPricingService
{
    /// <summary>
    /// Politika popusta kad za datum preuzimanja nema definisane sezone. Iste
    /// vrijednosti stoje i u seed cjenovniku - ovo je zastita da najam od sedam dana
    /// ne ostane bez popusta samo zato sto neko nije unio tarifu za taj period.
    /// </summary>
    private const int PodrazumijevaniPrag1 = 3;
    private const decimal PodrazumijevaniProcenat1 = 5m;
    private const int PodrazumijevaniPrag2 = 7;
    private const decimal PodrazumijevaniProcenat2 = 10m;

    private readonly SunnyRidesDbContext _context;
    private readonly ICjenovnikService _cjenovnikService;

    public PricingService(SunnyRidesDbContext context, ICjenovnikService cjenovnikService)
    {
        _context = context;
        _cjenovnikService = cjenovnikService;
    }

    public async Task<CijenaRezervacijeDto> IzracunajAsync(
        int voziloId,
        DateTime datumOd,
        DateTime datumDo,
        IReadOnlyList<StavkaOpremeRequest> oprema,
        int? paketOsiguranjaId,
        CancellationToken ct = default)
    {
        var vozilo = await _context.Vozila
            .Include(x => x.ModelVozila)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == voziloId, ct)
            ?? throw NotFoundException.Za("Vozilo", voziloId);

        if (!vozilo.Aktivno)
        {
            throw new BusinessException("Vozilo je povuceno iz ponude i ne moze se rezervisati.");
        }

        // Sezona se bira po datumu preuzimanja, ne po danasnjem danu. Rezervacija
        // napravljena u maju za termin u julu placa se po ljetnoj tarifi.
        var sezona = await _cjenovnikService.VazeciAsync(vozilo.ModelVozilaId, datumOd, ct);

        var stavke = await UcitajOpremuAsync(oprema, ct);
        var osiguranje = await UcitajOsiguranjeAsync(paketOsiguranjaId, ct);

        var ulaz = new UlazObracuna(
            DatumOd: datumOd,
            DatumDo: datumDo,

            // Cjenovnik smije nadjacati tarifu vozila; kad je ne navede, vazi tarifa
            // upisana na samom primjerku.
            SatnaTarifa: sezona?.SatnaTarifa ?? vozilo.SatnaTarifa,
            DnevnaTarifa: sezona?.DnevnaTarifa ?? vozilo.DnevnaTarifa,

            Mnozilac: sezona?.Mnozilac ?? 1m,
            NazivSezone: sezona?.Naziv,

            PopustPrag1: sezona?.PopustPrag1 ?? PodrazumijevaniPrag1,
            PopustProcenat1: sezona?.PopustProcenat1 ?? PodrazumijevaniProcenat1,
            PopustPrag2: sezona?.PopustPrag2 ?? PodrazumijevaniPrag2,
            PopustProcenat2: sezona?.PopustProcenat2 ?? PodrazumijevaniProcenat2,

            IznosDepozita: vozilo.IznosDepozita,

            Oprema: stavke,
            PaketOsiguranjaId: osiguranje?.Id,
            PaketOsiguranjaNaziv: osiguranje?.Naziv,
            OsiguranjeCijenaPoDanu: osiguranje?.CijenaPoDanu ?? 0m);

        return ObracunCijene.Izracunaj(ulaz);
    }

    private async Task<List<StavkaOpremeUlaz>> UcitajOpremuAsync(
        IReadOnlyList<StavkaOpremeRequest> trazeno, CancellationToken ct)
    {
        if (trazeno.Count == 0)
        {
            return new List<StavkaOpremeUlaz>();
        }

        var idevi = trazeno.Select(x => x.VrstaOpremeId).ToList();

        if (idevi.Distinct().Count() != idevi.Count)
        {
            throw new BusinessException(
                "Ista vrsta opreme je navedena vise puta. Umjesto toga povecajte kolicinu.");
        }

        // Jedan upit za svu opremu, a ne po jedan po stavci - inace bi rezervacija
        // sa pet komada opreme radila pet odvojenih upita.
        var izBaze = await _context.VrsteOpreme
            .Where(x => idevi.Contains(x.Id))
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, ct);

        var stavke = new List<StavkaOpremeUlaz>();

        foreach (var stavka in trazeno)
        {
            if (!izBaze.TryGetValue(stavka.VrstaOpremeId, out var vrsta))
            {
                throw new BusinessException(
                    $"Oprema sa identifikatorom {stavka.VrstaOpremeId} ne postoji.");
            }

            stavke.Add(new StavkaOpremeUlaz(
                vrsta.Id, vrsta.Naziv, stavka.Kolicina, vrsta.CijenaPoDanu, vrsta.FiksnaCijena));
        }

        return stavke;
    }

    private async Task<Database.Entities.PaketOsiguranja?> UcitajOsiguranjeAsync(
        int? paketOsiguranjaId, CancellationToken ct)
    {
        if (!paketOsiguranjaId.HasValue)
        {
            return null;
        }

        return await _context.PaketiOsiguranja
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paketOsiguranjaId.Value, ct)
            ?? throw new BusinessException(
                $"Paket osiguranja sa identifikatorom {paketOsiguranjaId.Value} ne postoji.");
    }
}
