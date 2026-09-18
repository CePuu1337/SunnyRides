namespace SunnyRides.Services.Preporuke.Ml;

/// <summary>Koliko model grijesi, izrazeno u jedinici ocjene i u odnosu na pogadjanje prosjeka.</summary>
public record RezultatMjerenja(
    double Rmse, double Mae, double RKvadrat, int BrojMjerenja, double RmseOsnovni);

/// <summary>
/// Mjere greske nad parovima stvarno-predvidjeno.
///
/// Racunaju se ovdje, a ne kroz ML.NET, iz dva razloga. Prvo, mjeri se **ono sto se
/// stvarno servira**: predikcija se prije prikaza svodi na raspon 1-5, pa i greska mora
/// biti racunata nad svedenom vrijednoscu. Drugo, ovako se mjere mogu racunati nad
/// spojenim predikcijama iz vise prolaza unakrsne provjere, umjesto da se prosjecuju
/// brojevi iz pojedinacnih prolaza - prosjek R kvadrata po malim skupovima nije isto sto
/// i R kvadrat nad svim mjerenjima.
/// </summary>
public static class Mjere
{
    public static RezultatMjerenja Izracunaj(IReadOnlyList<(double Stvarno, double Predvidjeno)> parovi)
    {
        if (parovi.Count == 0)
        {
            return new RezultatMjerenja(0, 0, 0, 0, 0);
        }

        var zbirKvadrata = 0.0;
        var zbirApsolutnih = 0.0;

        foreach (var (stvarno, predvidjeno) in parovi)
        {
            var greska = stvarno - predvidjeno;

            zbirKvadrata += greska * greska;
            zbirApsolutnih += Math.Abs(greska);
        }

        var rmse = Math.Sqrt(zbirKvadrata / parovi.Count);
        var mae = zbirApsolutnih / parovi.Count;

        // R kvadrat poredi model sa najprostijim mogucim predvidjanjem - uvijek isti
        // prosjek. Nula znaci da je model tacno toliko dobar, pozitivno da je bolji,
        // negativno da je losiji. Kad su sve stvarne ocjene iste, poredjenja nema.
        var prosjek = parovi.Average(x => x.Stvarno);
        var ukupnoOdstupanje = parovi.Sum(x => (x.Stvarno - prosjek) * (x.Stvarno - prosjek));

        var rKvadrat = ukupnoOdstupanje > 0 ? 1 - (zbirKvadrata / ukupnoOdstupanje) : 0;

        // Greska koju bi imalo najprostije predvidjanje - uvijek isti prosjek. Vraca se
        // uz ostalo da se model ima sa cim uporediti: "RMSE 0,84" sam po sebi ne govori
        // nista dok se ne zna da je pogadjanje prosjeka 1,07.
        var rmseOsnovni = Math.Sqrt(ukupnoOdstupanje / parovi.Count);

        return new RezultatMjerenja(rmse, mae, rKvadrat, parovi.Count, rmseOsnovni);
    }
}
