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

    public async Task<SacuvanaSlika> SacuvajJavnoAsync(
        Stream sadrzaj, long duzinaBajta, string podfolder, CancellationToken ct = default)
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

        // Sadrzaj se prvo prepise u memoriju, da se moze i procitati unaprijed radi
        // provjere potpisa i zatim ponovo od pocetka ucitati. Velicina je vec
        // ogranicena, pa je to sigurno.
        using var bafer = new MemoryStream();
        await sadrzaj.CopyToAsync(bafer, ct);
        bafer.Position = 0;

        ProvjeriPotpis(bafer);
        bafer.Position = 0;

        var apsolutniFolder = Path.Combine(_opcije.JavniKorijen, NormalizujPodfolder(podfolder));
        var folderThumbova = Path.Combine(apsolutniFolder, "thumbs");
        Directory.CreateDirectory(folderThumbova);

        var naziv = $"{Guid.NewGuid():N}.jpg";

        // Ucitavanje kroz ImageSharp je druga, nezavisna provjera: sadrzaj koji ima
        // ispravan potpis a pokvarenu strukturu ovdje pada, prije nego ista dodirne disk.
        using var slika = await Image.LoadAsync(bafer, ct);

        slika.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaksimalnaStranica, MaksimalnaStranica)
        }));

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

            var apsolutna = UApsolutnu(putanja);
            if (apsolutna is null)
            {
                continue;
            }

            try
            {
                if (File.Exists(apsolutna))
                {
                    File.Delete(apsolutna);
                }
            }
            catch (IOException ex)
            {
                // Zapis u bazi je vec obrisan i to je ono sto korisnik vidi. Fajl koji
                // je ostao na disku je smece, ne kvar - zato se biljezi, a operacija
                // se ne obara.
                _logger.LogWarning(ex, "Fajl {Putanja} nije obrisan sa diska.", apsolutna);
            }
        }
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

    /// <summary>
    /// Pretvara web putanju u putanju na disku, uz provjeru da rezultat ostaje
    /// unutar javnog korijena. Bez te provjere bi vrijednost poput
    /// "/uploads/../../appsettings.json" brisala fajlove izvan foldera za slike.
    /// </summary>
    private string? UApsolutnu(string webPutanja)
    {
        if (!webPutanja.StartsWith(PohranaOpcije.JavniPrefiks + "/", StringComparison.Ordinal))
        {
            return null;
        }

        var relativna = webPutanja[(PohranaOpcije.JavniPrefiks.Length + 1)..]
            .Replace('/', Path.DirectorySeparatorChar);

        var puna = Path.GetFullPath(Path.Combine(_opcije.JavniKorijen, relativna));

        return puna.StartsWith(_opcije.JavniKorijen, StringComparison.Ordinal) ? puna : null;
    }

    private static string NormalizujPodfolder(string podfolder) =>
        podfolder.Trim('/', '\\');
}
