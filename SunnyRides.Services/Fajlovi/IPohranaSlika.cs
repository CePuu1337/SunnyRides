namespace SunnyRides.Services.Fajlovi;

/// <summary>Web putanje do snimljene slike i njenog thumbnaila.</summary>
public record SacuvanaSlika(string Putanja, string PutanjaThumbnail);

/// <summary>Sadrzaj privatnog fajla, spreman za slanje klijentu koji je dokazao vlasnistvo.</summary>
public record PrivatniFajl(Stream Sadrzaj, string ContentType, string NazivFajla);

/// <summary>
/// Snimanje i brisanje slika na disku. U bazu ide samo putanja - sadrzaj slike
/// nikad ne ulazi u tabelu ni u odgovor liste.
///
/// Javne i privatne slike imaju odvojene metode, ne zajednicku sa zastavicom. Tako
/// se ne moze desiti da neko previdi parametar i osjetljivu fotografiju snimi u
/// folder koji se posluzuje staticki.
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

    /// <summary>
    /// Snima sliku u privatni folder i vraca **kljuc**, ne URL.
    ///
    /// Kljuc je relativna putanja bez prefiksa (npr. <c>dozvole/12/a1b2.jpg</c>).
    /// Namjerno ne lici na adresu: do privatnog fajla se dolazi iskljucivo kroz
    /// endpoint koji provjerava vlasnistvo, pa vrijednost iz baze ne smije izgledati
    /// kao nesto sto se moze zalijepiti u preglednik.
    ///
    /// Thumbnail se ne pravi - mala verzija osjetljivog dokumenta je i dalje
    /// osjetljiv dokument, a nigdje se ne prikazuje u listi.
    /// </summary>
    Task<string> SacuvajPrivatnoAsync(
        Stream sadrzaj, long duzinaBajta, string podfolder, CancellationToken ct = default);

    void ObrisiPrivatno(params string?[] kljucevi);

    /// <summary>
    /// Otvara privatni fajl za citanje. Pozivalac je duzan prije ovoga provjeriti
    /// vlasnistvo nad resursom - ova metoda o tome ne odlucuje.
    /// </summary>
    Task<PrivatniFajl> OtvoriPrivatnoAsync(
        string kljuc, string nazivZaPreuzimanje, CancellationToken ct = default);
}
