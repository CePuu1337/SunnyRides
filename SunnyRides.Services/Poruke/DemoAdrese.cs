namespace SunnyRides.Services.Poruke;

/// <summary>
/// Prepoznaje email adrese koje ne pripadaju nikome stvarnom.
///
/// Demo podaci koriste iskljucivo domene rezervisane za primjere i testiranje (RFC 2606
/// i RFC 6761): <c>example.com</c>, <c>example.net</c>, <c>example.org</c> i sve pod
/// <c>.example</c>, <c>.test</c>, <c>.invalid</c> i <c>.localhost</c>. Na takvu adresu
/// mail nikad ne bi stigao, pa ga worker preusmjerava na adresu za testiranje. Stvarna
/// adresa - recimo ona kojom se neko registrovao kroz mobilnu aplikaciju - dobija svoj
/// mail normalno.
/// </summary>
public static class DemoAdrese
{
    private static readonly string[] TacneDomene = ["example.com", "example.net", "example.org"];

    private static readonly string[] RezervisaniVrhovi = [".example", ".test", ".invalid", ".localhost"];

    public static bool JeDemo(string? adresa)
    {
        if (string.IsNullOrWhiteSpace(adresa))
        {
            return false;
        }

        var majmun = adresa.LastIndexOf('@');

        if (majmun < 0 || majmun == adresa.Length - 1)
        {
            return false;
        }

        var domena = adresa[(majmun + 1)..].Trim().TrimEnd('.').ToLowerInvariant();

        return TacneDomene.Any(x => domena == x || domena.EndsWith("." + x, StringComparison.Ordinal))
               || RezervisaniVrhovi.Any(x => domena.EndsWith(x, StringComparison.Ordinal)
                                             || domena == x.TrimStart('.'));
    }
}
