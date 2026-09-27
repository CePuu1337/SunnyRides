using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Flota;

/// <summary>
/// Prosjecna ocjena i broj recenzija za vec ucitana vozila.
///
/// Dopisuje se posebnim upitom, nakon sto je lista vec suzena. Include nad recenzijama
/// bi povukao svaki komentar svakog vozila samo da bi se izracunao prosjek; ovako se za
/// cijelu listu racuna jedan grupisani upit nad ocjenama, bez teksta. Koriste ga i
/// pretraga i preporuke, pa kartica vozila svuda prikazuje isti broj.
/// </summary>
public static class OcjeneVozila
{
    public static async Task DopuniAsync(
        SunnyRidesDbContext context, IReadOnlyCollection<VoziloDto> vozila, CancellationToken ct)
    {
        if (vozila.Count == 0)
        {
            return;
        }

        var ids = vozila.Select(x => x.Id).Distinct().ToList();

        // Skrivene recenzije ne ulaze u prosjek - to je i razlog zbog kojeg se
        // recenzija skriva umjesto da se brise.
        var ocjene = await context.Recenzije
            .Where(x => ids.Contains(x.VoziloId) && !x.Skrivena)
            .GroupBy(x => x.VoziloId)
            .Select(g => new
            {
                VoziloId = g.Key,
                Prosjek = g.Average(x => (double)x.Ocjena),
                Broj = g.Count(),
            })
            .ToDictionaryAsync(x => x.VoziloId, ct);

        foreach (var vozilo in vozila)
        {
            if (!ocjene.TryGetValue(vozilo.Id, out var stavka))
            {
                continue;
            }

            vozilo.ProsjecnaOcjena = Math.Round(stavka.Prosjek, 1);
            vozilo.BrojRecenzija = stavka.Broj;
        }
    }
}
