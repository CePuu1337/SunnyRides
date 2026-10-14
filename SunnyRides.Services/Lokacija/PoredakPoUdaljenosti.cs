using SunnyRides.Services.Preporuke;

namespace SunnyRides.Services.Lokacija;

/// <summary>
/// Redoslijed rezultata pretrage kad je odabran poredak "Najblize meni".
///
/// Vozila idu po poslovnicama, od najblize prema daljoj. Unutar iste poslovnice
/// redoslijed ostaje onaj iz sistema preporuke, sa istim pravilom da se primjerci
/// istog modela ne slazu jedan do drugog. Tako klijent prvo vidi ono sto moze
/// preuzeti najblize, a medju tim ono sto mu najvise odgovara.
///
/// Poslovnice bez koordinata nemaju udaljenost i idu na kraj. Racun je cist, pa ga
/// pokrivaju unit testovi.
/// </summary>
public static class PoredakPoUdaljenosti
{
    public static List<int> Poredaj(
        IEnumerable<RangiranoVozilo> vozila,
        IReadOnlyDictionary<int, int> poslovnicaVozila,
        IReadOnlyDictionary<int, double> udaljenostPoslovnica)
    {
        return vozila
            .GroupBy(x => poslovnicaVozila[x.VoziloId])
            .OrderBy(poslovnica => udaljenostPoslovnica.TryGetValue(poslovnica.Key, out var km)
                ? km
                : double.MaxValue)

            // Dvije poslovnice na istoj udaljenosti uvijek u istom redoslijedu, da se
            // stranice pri listanju ne mijesaju.
            .ThenBy(poslovnica => poslovnica.Key)
            .SelectMany(poslovnica => PoredakPoPreporuci.Poredaj(poslovnica))
            .ToList();
    }
}
