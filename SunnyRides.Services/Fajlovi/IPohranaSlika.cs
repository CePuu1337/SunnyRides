namespace SunnyRides.Services.Fajlovi;

/// <summary>Web putanje do snimljene slike i njenog thumbnaila.</summary>
public record SacuvanaSlika(string Putanja, string PutanjaThumbnail);

/// <summary>
/// Snimanje i brisanje slika na disku. U bazu ide samo putanja - sadrzaj slike
/// nikad ne ulazi u tabelu ni u odgovor liste.
/// </summary>
public interface IPohranaSlika
{
    /// <summary>
    /// Validira sadrzaj, smanjuje sliku, generise thumbnail i oboje snima u javni
    /// folder. Vraca putanje kakve klijent moze otvoriti.
    /// </summary>
    Task<SacuvanaSlika> SacuvajJavnoAsync(
        Stream sadrzaj, long duzinaBajta, string podfolder, CancellationToken ct = default);

    /// <summary>Brise fajlove po web putanjama. Nepostojeci fajl se preskace.</summary>
    void ObrisiJavno(params string?[] putanje);
}
