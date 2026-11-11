using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Flota;

/// <summary>
/// Je li vozilo u ovom trenutku na servisu ili u kvaru, za vec ucitana vozila.
///
/// Isti obrazac kao <see cref="OcjeneVozila"/>: dopisuje se posebnim upitom nad
/// blokadama vozila sa stranice, poslije paginacije, pa lista ne vuce historiju
/// blokada. Racuna se na serveru, a aplikacija samo prikazuje oznaku.
/// </summary>
public static class BlokadeVozila
{
    public static async Task DopuniAsync(
        SunnyRidesDbContext context, IReadOnlyCollection<VoziloDto> vozila, DateTime sada, CancellationToken ct)
    {
        if (vozila.Count == 0)
        {
            return;
        }

        var ids = vozila.Select(x => x.Id).Distinct().ToList();

        // Samo blokade koje traju sada. Vozilo ih istovremeno ima rijetko vise od jedne,
        // pa se redovi ucitaju i najduza se bira ovdje.
        var aktivne = await context.BlokadeVozila
            .AsNoTracking()
            .Where(x => ids.Contains(x.VoziloId) && x.DatumOd <= sada && x.DatumDo > sada)
            .Select(x => new { x.VoziloId, x.DatumDo, x.Razlog })
            .ToListAsync(ct);

        var poVozilu = aktivne
            .GroupBy(x => x.VoziloId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DatumDo).First());

        foreach (var vozilo in vozila)
        {
            if (poVozilu.TryGetValue(vozilo.Id, out var blokada))
            {
                vozilo.NaServisuDo = blokada.DatumDo;
                vozilo.RazlogBlokade = blokada.Razlog;
            }
        }
    }
}
