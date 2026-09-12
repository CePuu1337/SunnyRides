namespace SunnyRides.Services.Fajlovi;

/// <summary>
/// Prepoznavanje formata slike po prvim bajtima sadrzaja.
///
/// Ekstenzija i Content-Type zaglavlje dolaze od klijenta i oboje se slobodno
/// falsifikuju - "virus.exe" preimenovan u "slika.jpg" prolazi svaku provjeru koja
/// gleda samo naziv. Prvi bajtovi su dio samog sadrzaja i njih napadac ne moze
/// promijeniti a da fajl ostane ono sto tvrdi da jeste.
/// </summary>
public static class TipSlike
{
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] Riff = { 0x52, 0x49, 0x46, 0x46 };            // "RIFF"
    private static readonly byte[] Webp = { 0x57, 0x45, 0x42, 0x50 };            // "WEBP"

    /// <summary>Naziv formata, ili null ako sadrzaj nije podrzana slika.</summary>
    public static string? Prepoznaj(ReadOnlySpan<byte> pocetak)
    {
        if (Pocinje(pocetak, Jpeg)) { return "jpeg"; }
        if (Pocinje(pocetak, Png)) { return "png"; }

        // WEBP: bajtovi 0-3 su "RIFF", 4-7 su duzina, 8-11 su "WEBP".
        if (pocetak.Length >= 12 && Pocinje(pocetak, Riff) && pocetak[8..12].SequenceEqual(Webp))
        {
            return "webp";
        }

        return null;
    }

    /// <summary>Koliko bajta treba procitati da se format moze prepoznati.</summary>
    public const int PotrebnoBajta = 12;

    private static bool Pocinje(ReadOnlySpan<byte> sadrzaj, ReadOnlySpan<byte> potpis) =>
        sadrzaj.Length >= potpis.Length && sadrzaj[..potpis.Length].SequenceEqual(potpis);
}
