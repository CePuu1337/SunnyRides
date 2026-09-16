using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Fajlovi;

public class PohranaSlika : IPohranaSlika
{
    // Originali se ne cuvaju u velicini u kojoj su otpremljeni. Fotografija sa
    // telefona zna biti dvanaest megabajta, a na ekranu se nikad ne vidi vise od
    // par stotina piksela - cuvanje originala samo puni disk i usporava galeriju.
    private const int MaksimalnaStranica = 1600;
    private const int SirinaThumbnaila = 200;
    private const int VisinaThumbnaila = 150;

    private readonly PohranaOpcije _opcije;
    private readonly ILogger<PohranaSlika> _logger;

    public PohranaSlika(PohranaOpcije opcije, ILogger<PohranaSlika> logger)
    {
        _opcije = opcije;
        _logger = logger;
    }

    // --- javne slike -------------------------------------------------------

    public async Task<SacuvanaSlika> SacuvajJavnoAsync(
        Stream sadrzaj, long duzinaBajta, string podfolder, CancellationToken ct = default)
    {
        using var slika = await UcitajProvjerenuAsync(sadrzaj, duzinaBajta, ct);

        var apsolutniFolder = Path.Combine(_opcije.JavniKorijen, NormalizujPodfolder(podfolder));
        var folderThumbova = Path.Combine(apsolutniFolder, "thumbs");
        Directory.CreateDirectory(folderThumbova);

        var naziv = $"{Guid.NewGuid():N}.jpg";

        await slika.SaveAsJpegAsync(
            Path.Combine(apsolutniFolder, naziv), new JpegEncoder { Quality = 85 }, ct);

        // Thumbnail se sijece na tacan omjer, da kartice u listi budu iste visine.
        using var thumbnail = slika.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Crop,
            Size = new Size(SirinaThumbnaila, VisinaThumbnaila)
        }));

        await thumbnail.SaveAsJpegAsync(
            Path.Combine(folderThumbova, naziv), new JpegEncoder { Quality = 80 }, ct);

        var webFolder = $"{PohranaOpcije.JavniPrefiks}/{NormalizujPodfolder(podfolder).Replace('\\', '/')}";

        return new SacuvanaSlika($"{webFolder}/{naziv}", $"{webFolder}/thumbs/{naziv}");
    }

    public void ObrisiJavno(params string?[] putanje)
    {
        foreach (var putanja in putanje)
        {
            if (string.IsNullOrWhiteSpace(putanja))
            {
                continue;
            }

            if (!putanja.StartsWith(PohranaOpcije.JavniPrefiks + "/", StringComparison.Ordinal))
            {
                continue;
            }

            var relativna = putanja[(PohranaOpcije.JavniPrefiks.Length + 1)..];
            Obrisi(UApsolutnu(_opcije.JavniKorijen, relativna));
        }
    }

    // --- privatne slike ----------------------------------------------------

    public async Task<string> SacuvajPrivatnoAsync(
        Stream sadrzaj, long duzinaBajta, string podfolder, CancellationToken ct = default)
    {
        using var slika = await UcitajProvjerenuAsync(sadrzaj, duzinaBajta, ct);

        var relativniFolder = NormalizujPodfolder(podfolder);
        var apsolutniFolder = Path.Combine(_opcije.PrivatniKorijen, relativniFolder);
        Directory.CreateDirectory(apsolutniFolder);

        var naziv = $"{Guid.NewGuid():N}.jpg";

        await slika.SaveAsJpegAsync(
            Path.Combine(apsolutniFolder, naziv), new JpegEncoder { Quality = 85 }, ct);

        // Vraca se kljuc, ne adresa. Vrijednost koja zavrsi u bazi ne smije izgledati
        // kao nesto sto se moze otvoriti direktno.
        return $"{relativniFolder.Replace('\\', '/')}/{naziv}";
    }

    public void ObrisiPrivatno(params string?[] kljucevi)
    {
        foreach (var kljuc in kljucevi)
        {
            if (string.IsNullOrWhiteSpace(kljuc))
            {
                continue;
            }

            Obrisi(UApsolutnu(_opcije.PrivatniKorijen, kljuc));
        }
    }

    public Task<PrivatniFajl> OtvoriPrivatnoAsync(
        string kljuc, string nazivZaPreuzimanje, CancellationToken ct = default)
    {
        var apsolutna = UApsolutnu(_opcije.PrivatniKorijen, kljuc);

        if (apsolutna is null || !File.Exists(apsolutna))
        {
            throw new NotFoundException("Fotografija nije pronadjena.");
        }

        Stream sadrzaj = new FileStream(
            apsolutna, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult(new PrivatniFajl(sadrzaj, "image/jpeg", nazivZaPreuzimanje));
    }

    // --- zajednicko --------------------------------------------------------

    /// <summary>
    /// Provjerava velicinu i potpis sadrzaja, pa ga ucitava i smanjuje.
    ///
    /// Provjera je dvostruka i namjerno. Potpis je prva, jeftina kapija - ekstenzija
    /// i Content-Type dolaze od klijenta i oboje se falsifikuju. Ucitavanje kroz
    /// ImageSharp je druga, nezavisna: sadrzaj sa ispravnim potpisom a pokvarenom
    /// strukturom pada ovdje, prije nego ista dodirne disk.
    /// </summary>
    private static async Task<Image> UcitajProvjerenuAsync(
        Stream sadrzaj, long duzinaBajta, CancellationToken ct)
    {
        if (duzinaBajta <= 0)
        {
            throw new BusinessException("Fajl je prazan.");
        }

        if (duzinaBajta > PohranaOpcije.MaksimalnaVelicinaBajta)
        {
            var mb = PohranaOpcije.MaksimalnaVelicinaBajta / 1024 / 1024;
            throw new BusinessException($"Slika ne smije biti veca od {mb} MB.");
        }

        using var bafer = new MemoryStream();
        await sadrzaj.CopyToAsync(bafer, ct);

        bafer.Position = 0;
        ProvjeriPotpis(bafer);

        bafer.Position = 0;
        var slika = await Image.LoadAsync(bafer, ct);

        slika.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaksimalnaStranica, MaksimalnaStranica)
        }));

        return slika;
    }

    private static void ProvjeriPotpis(Stream sadrzaj)
    {
        Span<byte> pocetak = stackalloc byte[TipSlike.PotrebnoBajta];
        var procitano = sadrzaj.Read(pocetak);

        if (TipSlike.Prepoznaj(pocetak[..procitano]) is null)
        {
            throw new BusinessException(
                "Dozvoljene su samo slike u formatu JPEG, PNG ili WEBP.");
        }
    }

    private void Obrisi(string? apsolutnaPutanja)
    {
        if (apsolutnaPutanja is null)
        {
            return;
        }

        try
        {
            if (File.Exists(apsolutnaPutanja))
            {
                File.Delete(apsolutnaPutanja);
            }
        }
        catch (IOException ex)
        {
            // Zapis u bazi je vec obrisan i to je ono sto korisnik vidi. Fajl koji
            // je ostao na disku je smece, ne kvar - zato se biljezi, a operacija
            // se ne obara.
            _logger.LogWarning(ex, "Fajl {Putanja} nije obrisan sa diska.", apsolutnaPutanja);
        }
    }

    /// <summary>
    /// Spaja korijen i relativnu putanju, uz provjeru da rezultat ostaje unutar tog
    /// korijena. Bez te provjere bi vrijednost poput <c>../../appsettings.json</c>
    /// izasla iz foldera za slike - a kod privatnog korijena to znaci citanje fajlova
    /// koje niko ne bi smio vidjeti.
    /// </summary>
    private static string? UApsolutnu(string korijen, string relativna)
    {
        var ocisceno = relativna.Replace('/', Path.DirectorySeparatorChar).TrimStart(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var puna = Path.GetFullPath(Path.Combine(korijen, ocisceno));

        var granica = korijen.EndsWith(Path.DirectorySeparatorChar)
            ? korijen
            : korijen + Path.DirectorySeparatorChar;

        return puna.StartsWith(granica, StringComparison.Ordinal) ? puna : null;
    }

    private static string NormalizujPodfolder(string podfolder) =>
        podfolder.Trim('/', '\\');
}
