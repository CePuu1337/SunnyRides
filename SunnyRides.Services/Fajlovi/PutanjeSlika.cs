namespace SunnyRides.Services.Fajlovi;

/// <summary>
/// Izvodjenje putanje thumbnaila iz putanje slike.
///
/// Postoji zato sto neki entiteti cuvaju samo jednu putanju, a prikaz u listi treba
/// malu sliku. Pravilo je ono isto koje <see cref="PohranaSlika"/> koristi pri snimanju
/// - thumbnail stoji u podfolderu "thumbs" pored originala - pa je na jednom mjestu
/// napisano i vidljivo, umjesto da se pogadja na tri mjesta u kodu.
/// </summary>
public static class PutanjeSlika
{
    public const string FolderThumbova = "thumbs";

    public static string? Thumbnail(string? putanjaSlike)
    {
        if (string.IsNullOrWhiteSpace(putanjaSlike))
        {
            return null;
        }

        var granica = putanjaSlike.LastIndexOf('/');

        if (granica < 0)
        {
            return $"{FolderThumbova}/{putanjaSlike}";
        }

        var folder = putanjaSlike[..granica];
        var naziv = putanjaSlike[(granica + 1)..];

        return $"{folder}/{FolderThumbova}/{naziv}";
    }
}
