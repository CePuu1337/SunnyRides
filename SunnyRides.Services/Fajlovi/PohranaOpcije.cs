namespace SunnyRides.Services.Fajlovi;

/// <summary>
/// Gdje na disku zive otpremljeni fajlovi i pod kojim ih prefiksom klijent vidi.
///
/// Postoje dva korijena i razlika medju njima je sigurnosna, ne organizaciona:
///
/// <list type="bullet">
/// <item><b>Javni</b> - fotografije vozila i obavijesti. Posluzuju se kao staticki
/// fajlovi, bez tokena, jer su to katalog i oglasi agencije.</item>
/// <item><b>Privatni</b> - fotografije vozackih dozvola i stete. Nikad se ne
/// posluzuju staticki; do njih se dolazi iskljucivo kroz endpoint koji provjerava
/// vlasnistvo nad resursom.</item>
/// </list>
///
/// Zato privatni fajlovi ne smiju zavrsiti nigdje ispod javnog korijena. Da su u
/// istom stablu, jedan red konfiguracije statickih fajlova bio bi dovoljan da fotografija
/// tudje vozacke dozvole postane dostupna svakome ko pogodi putanju.
/// </summary>
public class PohranaOpcije
{
    /// <summary>Folder na disku koji se posluzuje staticki.</summary>
    public required string JavniKorijen { get; init; }

    /// <summary>Folder na disku koji se nikad ne posluzuje staticki.</summary>
    public required string PrivatniKorijen { get; init; }

    /// <summary>URL prefiks pod kojim klijent vidi javne fajlove.</summary>
    public const string JavniPrefiks = "/uploads";

    /// <summary>Najveca dozvoljena velicina otpremljenog fajla.</summary>
    public const long MaksimalnaVelicinaBajta = 5 * 1024 * 1024;

    private const string NazivJavnog = "uploads";
    private const string NazivPrivatnog = "privatno";

    /// <summary>
    /// Pronalazi oba korijena bez rucnog podesavanja, i to kao **susjedne** foldere
    /// ispod istog roditelja.
    ///
    /// Trazi se samo javni folder, jer on postoji u repozitoriju - privatni se pravi
    /// pri prvom pokretanju i zato ga nema smisla traziti. Da se svaki trazi zasebno,
    /// javni bi se nasao u korijenu repozitorija a privatni bi ispao u folderu API
    /// projekta, jer tamo jos ne postoji nijedan.
    ///
    /// U kontejneru je radni folder <c>/app</c>, a Compose u njega montira oba;
    /// pri lokalnom <c>dotnet run</c> radni folder je SunnyRides.API, a oba su jedan
    /// nivo iznad. Isti kod radi u oba okruzenja bez ijedne izmjene.
    /// </summary>
    public static PohranaOpcije IzOkruzenja()
    {
        var korijen = Environment.GetEnvironmentVariable("DATA_ROOT") ?? PronadjiKorijen();

        var javni = Environment.GetEnvironmentVariable("UPLOADS_ROOT")
                    ?? Path.Combine(korijen, NazivJavnog);

        var privatni = Environment.GetEnvironmentVariable("PRIVATE_UPLOADS_ROOT")
                       ?? Path.Combine(korijen, NazivPrivatnog);

        Directory.CreateDirectory(javni);
        Directory.CreateDirectory(privatni);

        return new PohranaOpcije
        {
            JavniKorijen = Path.GetFullPath(javni),
            PrivatniKorijen = Path.GetFullPath(privatni)
        };
    }

    private static string PronadjiKorijen()
    {
        var radni = Directory.GetCurrentDirectory();

        if (Directory.Exists(Path.Combine(radni, NazivJavnog)))
        {
            return radni;
        }

        var iznad = Path.Combine(radni, "..");

        return Directory.Exists(Path.Combine(iznad, NazivJavnog)) ? iznad : radni;
    }
}
