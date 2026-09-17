using Microsoft.EntityFrameworkCore;
using SunnyRides.Services.Database;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Zakljucavanje reda rezervacije do kraja tekuce transakcije.
///
/// Rezervaciju mijenja vise puteva koji mogu stici istovremeno: klijent potvrdjuje
/// placanje, Stripe salje webhook za isto placanje, klijent ili agencija otkazuje.
/// Svaki od njih prvo zakljuca red, pa tek onda cita stanje - drugi ceka dok prvi ne
/// zavrsi i onda vidi vec promijenjeno stanje, umjesto da oba rade nad starim.
/// </summary>
public static class ZakljucavanjeRezervacije
{
    public static async Task ZakljucajRezervacijuAsync(
        this SunnyRidesDbContext context, int rezervacijaId, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT TOP 1 Id FROM Rezervacija WITH (UPDLOCK, HOLDLOCK) WHERE Id = {rezervacijaId}", ct);
    }
}
