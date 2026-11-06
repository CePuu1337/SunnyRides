namespace SunnyRides.Services.Oprema;

/// <summary>Jedna rezervacija koja drzi odredjen broj komada opreme od preuzimanja do vracanja.</summary>
public readonly record struct Zaduzenje(DateTime Od, DateTime Do, int Kolicina);

/// <summary>Najveci broj komada koji je u jednom trenutku kod klijenata, i od kada.</summary>
public readonly record struct Vrhunac(int Kolicina, DateTime? Od);

/// <summary>
/// Koliko je komada jedne vrste opreme obecano u isto vrijeme.
///
/// Rezervacije koje se preklapaju sa trazenim terminom ne moraju se preklapati
/// medjusobno. Ako jedna kaciga ide od ponedjeljka do srijede, a druga od cetvrtka
/// do subote, poslovnica sa jednom kacigom pokriva obje - i treca rezervacija za
/// cijelu sedmicu treba vidjeti da je zauzeta jedna, a ne dvije. Zato se kolicine
/// ne sabiraju naslijepo, nego se trazi trenutak u kojem je kod klijenata najvise
/// komada istovremeno.
///
/// Racuna se prolaskom kroz vremensku liniju: svako preuzimanje dodaje svoju
/// kolicinu, svako vracanje je oduzima, a pamti se najveci zbir. Kad se u istom
/// trenutku jedna rezervacija zavrsava a druga pocinje, vracanje ide prvo - termin
/// je poluotvoren interval, kao i u provjeri dostupnosti vozila.
///
/// Nije bitno da li se vrhunac desi unutar trazenog termina ili malo izvan njega:
/// ako se nekoliko intervala medjusobno preklapa i svaki dira trazeni termin, onda
/// imaju i zajednicku tacku unutar njega. Zato je dovoljno prebrojati samo
/// rezervacije koje dodiruju trazeni termin.
/// </summary>
public static class IstovremenaOprema
{
    public static Vrhunac Najvise(IEnumerable<Zaduzenje> zaduzenja)
    {
        var dogadjaji = zaduzenja
            .Where(x => x.Kolicina > 0 && x.Do > x.Od)
            .SelectMany(x => new[] { (Trenutak: x.Od, Promjena: x.Kolicina), (Trenutak: x.Do, Promjena: -x.Kolicina) })
            .OrderBy(x => x.Trenutak)
            .ThenBy(x => x.Promjena);

        var trenutno = 0;
        var najvise = 0;
        DateTime? od = null;

        foreach (var (trenutak, promjena) in dogadjaji)
        {
            trenutno += promjena;

            if (trenutno > najvise)
            {
                najvise = trenutno;
                od = trenutak;
            }
        }

        return new Vrhunac(najvise, od);
    }

    /// <summary>
    /// Zasto trazena kolicina ne moze, ili null kad moze. Poruka kaze koliko je
    /// slobodno, da klijent ili uposlenik zna na koliko da smanji, a ne samo da ne moze.
    /// </summary>
    public static string? Razlog(string naziv, string poslovnica, int naStanju, int zauzeto, int trazeno)
    {
        if (naStanju <= 0)
        {
            return $"Oprema \"{naziv}\" nije dostupna u poslovnici {poslovnica}.";
        }

        var slobodno = naStanju - zauzeto;

        if (slobodno <= 0)
        {
            return $"Sva oprema \"{naziv}\" u poslovnici {poslovnica} je u tom terminu vec rezervisana.";
        }

        if (trazeno > slobodno)
        {
            return $"Za opremu \"{naziv}\" u poslovnici {poslovnica} je u tom terminu slobodno jos {slobodno} kom.";
        }

        return null;
    }
}
