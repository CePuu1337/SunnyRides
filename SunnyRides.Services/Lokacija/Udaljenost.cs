namespace SunnyRides.Services.Lokacija;

/// <summary>
/// Zracna udaljenost dvije tacke na Zemlji, po haversine formuli.
///
/// Nije udaljenost putem - za to bi trebao vanjski servis za rute. Za pitanje koja je
/// poslovnica blize daje isti odgovor u gotovo svakom slucaju, a racun je cist, bez
/// baze i mreze, pa ga pokrivaju unit testovi.
/// </summary>
public static class Udaljenost
{
    /// <summary>Srednji poluprecnik Zemlje. Greska zbog spljostenosti je ispod pola posto.</summary>
    private const double PoluprecnikZemljeKm = 6371.0;

    public static double Km(double latituda1, double longituda1, double latituda2, double longituda2)
    {
        var razlikaSirine = URadijane(latituda2 - latituda1);
        var razlikaDuzine = URadijane(longituda2 - longituda1);

        var a = Math.Pow(Math.Sin(razlikaSirine / 2), 2)
                + Math.Cos(URadijane(latituda1)) * Math.Cos(URadijane(latituda2))
                  * Math.Pow(Math.Sin(razlikaDuzine / 2), 2);

        // Zaokruzivanje moze dati a malo iznad 1 za suprotne tacke, a Asin je tada NaN.
        return 2 * PoluprecnikZemljeKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double URadijane(double stepeni) => stepeni * Math.PI / 180;
}
