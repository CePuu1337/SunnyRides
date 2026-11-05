namespace SunnyRides.Services.Dozvole;

/// <summary>
/// Da li model vozila odgovara kategoriji dozvole koja mu je upisana.
///
/// Pretraga klijentu nudi vozila po kategoriji upisanoj na modelu. Ako bi model od
/// 649 ccm bio upisan kao A1, klijent sa A1 bi ga vidio i rezervisao, iako ga po
/// pravilu kategorije ne smije voziti. Zato server pri unosu i izmjeni modela
/// provjerava da kubikaza i snaga ne prelaze granice iz pravila kategorije za taj tip
/// vozila, a pri izmjeni pravila da nijedan postojeci model ne ostane izvan njih.
///
/// Klasa je bez baze, da se pravilo moze testirati samo.
/// </summary>
public static class UskladjenostModela
{
    /// <summary>
    /// Razlog zasto model ne odgovara kategoriji, ili null kad odgovara.
    /// <paramref name="pravilo"/> je pravilo kategorije za tip vozila tog modela; null
    /// znaci da kategorija za taj tip nema pravila.
    /// </summary>
    public static string? Razlog(
        string oznakaKategorije, string nazivTipa, int kubikaza, decimal snagaKw, PraviloUlaz? pravilo)
    {
        var tip = nazivTipa.ToLowerInvariant();

        if (pravilo is null)
        {
            return $"Kategorija {oznakaKategorije} nema pravilo za tip vozila \"{nazivTipa}\", pa niko sa " +
                   "tom kategorijom ne bi smio voziti ovaj model. Odaberite drugu kategoriju ili prvo " +
                   "dodajte pravilo u sifarniku Pravila kategorija.";
        }

        if (PrelaziKubikazu(pravilo.MaxKubikaza, kubikaza))
        {
            return $"Kategorija {oznakaKategorije} za tip \"{tip}\" dozvoljava najvise {pravilo.MaxKubikaza} ccm, " +
                   $"a ovaj model ima {kubikaza} ccm. Odaberite visu kategoriju.";
        }

        if (PrelaziSnagu(pravilo.MaxSnagaKw, snagaKw))
        {
            return $"Kategorija {oznakaKategorije} za tip \"{tip}\" dozvoljava najvise {pravilo.MaxSnagaKw:0.#} kW, " +
                   $"a ovaj model ima {snagaKw:0.#} kW. Odaberite visu kategoriju.";
        }

        return null;
    }

    /// <summary>Prazna granica znaci "bez ogranicenja".</summary>
    public static bool PrelaziKubikazu(int? granica, int kubikaza) => granica is { } g && kubikaza > g;

    /// <summary>Prazna granica znaci "bez ogranicenja".</summary>
    public static bool PrelaziSnagu(decimal? granica, decimal snagaKw) => granica is { } g && snagaKw > g;
}
