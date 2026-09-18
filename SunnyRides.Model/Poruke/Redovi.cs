namespace SunnyRides.Model.Poruke;

/// <summary>
/// Nazivi redova na RabbitMQ-u. Stoje u Model projektu zato sto ih koriste i API,
/// koji poruke salje, i worker, koji ih obradjuje - a jedini nacin da se ne raziđu
/// jeste da ih obje strane citaju sa istog mjesta.
/// </summary>
public static class Redovi
{
    public const string RezervacijaKreirana = "rezervacija.kreirana";
    public const string PlacanjeUspjesno = "placanje.uspjesno";
    public const string RezervacijaOtkazana = "rezervacija.otkazana";
    public const string PovratIzvrsen = "povrat.izvrsen";
    public const string DozvolaVerifikovana = "dozvola.verifikovana";
    public const string PodsjetnikPreuzimanje = "podsjetnik.preuzimanje";
    public const string VoziloVraceno = "vozilo.vraceno";
    public const string ResetLozinke = "reset.lozinke";

    /// <summary>Worker po ovoj listi pravi redove i pretplacuje se na njih.</summary>
    public static readonly IReadOnlyList<string> Sve = new[]
    {
        RezervacijaKreirana,
        PlacanjeUspjesno,
        RezervacijaOtkazana,
        PovratIzvrsen,
        DozvolaVerifikovana,
        PodsjetnikPreuzimanje,
        VoziloVraceno,
        ResetLozinke
    };

    /// <summary>
    /// Redovi cije poruke ne smiju zavrsiti u logu.
    ///
    /// Poruka o resetu lozinke nosi kod u citljivom obliku, a log cita vise ljudi nego
    /// bazu i najcesce zavrsi negdje gdje se cuva duze nego sto kod vazi. Zato se za
    /// ove redove logira samo naziv reda, ne i sadrzaj.
    /// </summary>
    public static bool SadrziTajnu(string red) => red == ResetLozinke;
}
