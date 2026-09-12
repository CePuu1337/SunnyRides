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

    /// <summary>
    /// Pronalazi korijene bez rucnog podesavanja.
    ///
    /// U kontejneru je radni folder /app, a docker-compose u njega montira ./uploads,
    /// pa se folder nalazi odmah. Pri lokalnom "dotnet run" radni folder je
    /// SunnyRides.API, a folder je jedan nivo iznad. Ista logika kao kod .env fajla -
    /// isti kod radi u oba okruzenja bez ijedne izmjene.
    /// </summary>
    public static PohranaOpcije IzOkruzenja()
    {
        var javni = Environment.GetEnvironmentVariable("UPLOADS_ROOT")
                    ?? PronadjiIliNapravi("uploads");

        var privatni = Environment.GetEnvironmentVariable("PRIVATE_UPLOADS_ROOT")
                       ?? PronadjiIliNapravi("privatno");

        Directory.CreateDirectory(javni);
        Directory.CreateDirectory(privatni);

        return new PohranaOpcije
        {
            JavniKorijen = Path.GetFullPath(javni),
            PrivatniKorijen = Path.GetFullPath(privatni)
        };
    }

    private static string PronadjiIliNapravi(string naziv)
    {
        var uRadnom = Path.Combine(Directory.GetCurrentDirectory(), naziv);
        if (Directory.Exists(uRadnom))
        {
            return uRadnom;
        }

        var iznad = Path.Combine(Directory.GetCurrentDirectory(), "..", naziv);
        return Directory.Exists(iznad) ? iznad : uRadnom;
    }
}
