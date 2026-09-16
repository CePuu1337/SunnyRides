using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Tabela dozvoljenih prelaza statusa rezervacije.
///
/// Odvojena je od ostatka mehanizma i nema nijednu zavisnost - ni bazu, ni korisnika,
/// ni audit. Zbog toga se cijelo pravilo moze provjeriti kao obicna funkcija, a
/// odgovor na pitanje "smije li se iz X u Y" stoji na jednom mjestu koje se moze
/// procitati odjednom.
/// </summary>
public static class PrelaziRezervacije
{
    /// <summary>
    /// Sve sto je dozvoljeno. Sve sto ovdje ne pise je zabranjeno - nema implicitnih
    /// prelaza i nema prelaza koji se "podrazumijeva".
    /// </summary>
    private static readonly IReadOnlyDictionary<StatusRezervacije, StatusRezervacije[]> Dozvoljeni =
        new Dictionary<StatusRezervacije, StatusRezervacije[]>
        {
            // Placanje verifikovano na serveru, ili isteklo drzanje / odustajanje.
            [StatusRezervacije.Pending] = new[]
            {
                StatusRezervacije.Confirmed,
                StatusRezervacije.Cancelled
            },

            // Otkazivanje prije preuzimanja, ili evidentiran povrat vozila.
            [StatusRezervacije.Confirmed] = new[]
            {
                StatusRezervacije.Cancelled,
                StatusRezervacije.Completed
            },

            // Terminalni statusi. Otkazana se ne ozivljava, zavrsena se ne vraca.
            [StatusRezervacije.Cancelled] = Array.Empty<StatusRezervacije>(),
            [StatusRezervacije.Completed] = Array.Empty<StatusRezervacije>()
        };

    public static bool JeDozvoljen(StatusRezervacije iz, StatusRezervacije u) =>
        Dozvoljeni.TryGetValue(iz, out var moguci) && moguci.Contains(u);

    public static IReadOnlyList<StatusRezervacije> Iz(StatusRezervacije status) =>
        Dozvoljeni.TryGetValue(status, out var moguci) ? moguci : Array.Empty<StatusRezervacije>();

    public static bool JeTerminalan(StatusRezervacije status) => Iz(status).Count == 0;

    /// <summary>
    /// Naziv statusa u poruci koju cita korisnik. Enum ostaje na engleskom jer je
    /// tehnicki termin, ali poruka o gresci mora biti razumljiva.
    /// </summary>
    public static string Naziv(StatusRezervacije status) => status switch
    {
        StatusRezervacije.Pending => "na cekanju",
        StatusRezervacije.Confirmed => "potvrdjena",
        StatusRezervacije.Cancelled => "otkazana",
        StatusRezervacije.Completed => "zavrsena",
        _ => status.ToString()
    };

    /// <summary>Poruka koja objasnjava zasto prelaz nije moguc i sta jeste.</summary>
    public static string PorukaOdbijanja(StatusRezervacije iz, StatusRezervacije u)
    {
        if (JeTerminalan(iz))
        {
            return $"Rezervacija je {Naziv(iz)} i njen status se vise ne mijenja.";
        }

        var moguci = string.Join(", ", Iz(iz).Select(Naziv));

        return $"Rezervacija je {Naziv(iz)} i ne moze postati {Naziv(u)}. " +
               $"Iz ovog stanja moguce je samo: {moguci}.";
    }
}
