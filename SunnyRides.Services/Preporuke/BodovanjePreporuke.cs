namespace SunnyRides.Services.Preporuke;

/// <summary>Signal koji je najvise doprinio skoru. Od njega se gradi obrazlozenje uz preporuku.</summary>
public enum SignalPreporuke
{
    Tip = 1,
    Cijena = 2,
    Lokacija = 3,
    Marka = 4,
    Kubikaza = 5,
    Popularnost = 6,
    Ocjena = 7
}

/// <summary>
/// Ukus jednog korisnika, izveden iz njegovih pretraga i zavrsenih najmova.
///
/// Svaki rjecnik nosi udjele koji se sabiraju u 1 - koliko je puta korisnik trazio
/// bas tu vrijednost u odnosu na sve svoje zapise. Prazan rjecnik znaci da o tom
/// signalu nema podataka, sto nije isto sto i udio nula: nepoznat signal se izbacuje
/// iz racuna, a udio nula znaci da ga je korisnik imao priliku traziti i nije.
/// </summary>
public record ProfilKorisnika(
    IReadOnlyDictionary<int, double> Tipovi,
    IReadOnlyDictionary<int, double> Marke,
    IReadOnlyDictionary<int, double> Gradovi,
    IReadOnlyDictionary<int, double> KubikazniRazredi,
    decimal? ProsjecnaCijena)
{
    public static ProfilKorisnika Prazan { get; } = new(
        new Dictionary<int, double>(),
        new Dictionary<int, double>(),
        new Dictionary<int, double>(),
        new Dictionary<int, double>(),
        null);
}

/// <summary>Vozilo svedeno na atribute koji ulaze u bodovanje.</summary>
public record KandidatVozilo(
    int VoziloId,
    int ModelVozilaId,
    int TipVozilaId,
    int MarkaId,
    int GradId,
    int Kubikaza,
    decimal DnevnaTarifa);

/// <summary>Sirovi brojevi za jedno vozilo: najmovi u prozoru i zbir ocjena.</summary>
public record SignaliPopularnosti(int BrojNajmova, int BrojOcjena, double ZbirOcjena)
{
    public static SignaliPopularnosti Prazni { get; } = new(0, 0, 0);
}

/// <summary>
/// Okvir u kojem se popularnost mjeri: najveci broj najmova u floti (za normalizaciju)
/// i prosjecna ocjena cijele flote (prior za Bayesovu ocjenu).
/// </summary>
public record KontekstPopularnosti(int NajviseNajmova, double ProsjekOcjenaFlote);

public record RezultatPopularnosti(double Vrijednost, double Ucestalost, double Ocjena, double Bayes);

public record RezultatBodovanja(
    double Skor,
    double Slicnost,
    double Popularnost,
    SignalPreporuke Signal,
    double BayesovaOcjena,
    int BrojOcjena,
    int BrojNajmova);

/// <summary>
/// Bodovanje vozila za jednog korisnika.
///
/// Klasa nema bazu ni zavisnosti - prima gotove brojeve i vraca skor, pa se cijeli
/// model provjerava obicnim testovima. Sve tezine i granice su konstante ovdje, na
/// jednom mjestu, i iste vrijednosti stoje u <c>recommender-dokumentacija.md</c>.
/// Ako se razidju, dokument prestaje biti tacan opis koda.
/// </summary>
public static class BodovanjePreporuke
{
    // Tezine unutar slicnosti. Zbir je 1, ali se na to nigdje ne oslanja - racun
    // dijeli sa zbirom tezina onih signala o kojima profil zaista ima podatak.
    public const double TezinaTip = 0.35;
    public const double TezinaCijena = 0.25;
    public const double TezinaLokacija = 0.20;
    public const double TezinaMarka = 0.10;
    public const double TezinaKubikaza = 0.10;

    // Odnos dvije komponente konacnog skora.
    public const double UdioSlicnosti = 0.6;
    public const double UdioPopularnosti = 0.4;

    /// <summary>Prozor u kojem se broje najmovi za popularnost.</summary>
    public const int DanaZaPopularnost = 90;

    /// <summary>
    /// Koliko "zamisljenih" ocjena u prosjeku flote nosi svako vozilo prije nego dobije
    /// svoje. Bez toga bi vozilo sa jednom peticom bilo ispred vozila sa cetrdeset
    /// ocjena i prosjekom 4,7.
    /// </summary>
    public const double PriorOcjena = 5;

    public const double NajmanjaOcjena = 1;
    public const double NajvecaOcjena = 5;

    /// <summary>Prosjek koji se koristi dok u floti nema nijedne recenzije.</summary>
    public const double NeutralnaOcjena = 3;

    /// <summary>
    /// Razredi kubikaze. Granice prate ono sto stvarno razlikuje vozila u ovoj floti:
    /// mali gradski skuter, kategorija A1, srednji motocikl i sve preko toga.
    /// </summary>
    public static int RazredKubikaze(int kubikaza) =>
        kubikaza <= 50 ? 1
        : kubikaza <= 125 ? 2
        : kubikaza <= 500 ? 3
        : 4;

    /// <summary>
    /// Popularnost vozila: pola ucestalost najma, pola ocjena.
    ///
    /// Ucestalost je broj najmova podijeljen sa najvecim brojem u floti, pa je
    /// najtrazenije vozilo uvijek 1. Ocjena je Bayesov prosjek sveden na raspon 0-1.
    /// </summary>
    public static RezultatPopularnosti Popularnost(
        SignaliPopularnosti signali, KontekstPopularnosti kontekst)
    {
        var ucestalost = kontekst.NajviseNajmova > 0
            ? Math.Clamp((double)signali.BrojNajmova / kontekst.NajviseNajmova, 0, 1)
            : 0;

        var prosjekFlote = kontekst.ProsjekOcjenaFlote > 0
            ? kontekst.ProsjekOcjenaFlote
            : NeutralnaOcjena;

        var bayes = (PriorOcjena * prosjekFlote + signali.ZbirOcjena)
                    / (PriorOcjena + signali.BrojOcjena);

        var ocjena = Math.Clamp(
            (bayes - NajmanjaOcjena) / (NajvecaOcjena - NajmanjaOcjena), 0, 1);

        return new RezultatPopularnosti(0.5 * ucestalost + 0.5 * ocjena, ucestalost, ocjena, bayes);
    }

    /// <summary>
    /// Konacni skor jednog vozila za jednog korisnika.
    ///
    /// Korisnik bez ijednog signala u profilu dobija cistu popularnost - mnozenje sa
    /// 0,4 bi tada samo svima podjednako smanjilo skor, a poredak ostao isti, pa bi
    /// brojevi izgledali manji nego sto model zapravo tvrdi.
    /// </summary>
    public static RezultatBodovanja Boduj(
        ProfilKorisnika profil,
        KandidatVozilo vozilo,
        SignaliPopularnosti signali,
        KontekstPopularnosti kontekst)
    {
        var popularnost = Popularnost(signali, kontekst);

        double zbirTezina = 0;
        double zbirDoprinosa = 0;
        double najveciDoprinos = -1;
        var najjaciSignal = SignalPreporuke.Popularnost;

        void Dodaj(SignalPreporuke signal, double tezina, double? poklapanje)
        {
            // Signal o kojem profil nema podatak ne ulazi ni u brojnik ni u nazivnik.
            // Da ulazi kao nula, vozilo bi bilo kaznjeno zato sto korisnik o tome
            // nikad nije ostavio trag.
            if (poklapanje is null)
            {
                return;
            }

            var doprinos = tezina * poklapanje.Value;

            zbirTezina += tezina;
            zbirDoprinosa += doprinos;

            if (doprinos > najveciDoprinos)
            {
                najveciDoprinos = doprinos;
                najjaciSignal = signal;
            }
        }

        Dodaj(SignalPreporuke.Tip, TezinaTip, Udio(profil.Tipovi, vozilo.TipVozilaId));
        Dodaj(SignalPreporuke.Cijena, TezinaCijena, BlizinaCijene(profil.ProsjecnaCijena, vozilo.DnevnaTarifa));
        Dodaj(SignalPreporuke.Lokacija, TezinaLokacija, Udio(profil.Gradovi, vozilo.GradId));
        Dodaj(SignalPreporuke.Marka, TezinaMarka, Udio(profil.Marke, vozilo.MarkaId));
        Dodaj(SignalPreporuke.Kubikaza, TezinaKubikaza,
              Udio(profil.KubikazniRazredi, RazredKubikaze(vozilo.Kubikaza)));

        var imaProfil = zbirTezina > 0;
        var slicnost = imaProfil ? zbirDoprinosa / zbirTezina : 0;

        var skor = imaProfil
            ? UdioSlicnosti * slicnost + UdioPopularnosti * popularnost.Vrijednost
            : popularnost.Vrijednost;

        return new RezultatBodovanja(
            Skor: skor,
            Slicnost: slicnost,
            Popularnost: popularnost.Vrijednost,
            Signal: NajjaciSignal(imaProfil, najveciDoprinos, zbirTezina, popularnost, najjaciSignal),
            BayesovaOcjena: popularnost.Bayes,
            BrojOcjena: signali.BrojOcjena,
            BrojNajmova: signali.BrojNajmova);
    }

    /// <summary>
    /// Signal za obrazlozenje bira se poredjenjem doprinosa **konacnom** skoru, ne
    /// sirovih vrijednosti - inace bi popularnost od 0,9 uvijek pobijedila poklapanje
    /// tipa, iako u skor ulazi sa manjim udjelom.
    /// </summary>
    private static SignalPreporuke NajjaciSignal(
        bool imaProfil, double najveciDoprinos, double zbirTezina,
        RezultatPopularnosti popularnost, SignalPreporuke najjaciIzProfila)
    {
        var doprinosPopularnosti = imaProfil
            ? UdioPopularnosti * popularnost.Vrijednost
            : popularnost.Vrijednost;

        var doprinosProfila = imaProfil
            ? UdioSlicnosti * (najveciDoprinos / zbirTezina)
            : 0;

        if (!imaProfil || doprinosPopularnosti > doprinosProfila)
        {
            // Unutar popularnosti se razdvaja sta je vozilo dovelo gore: ucestalost
            // najma ili ocjene. Klijentu "najtrazenije" i "najbolje ocijenjeno" nisu
            // ista poruka.
            return popularnost.Ucestalost >= popularnost.Ocjena
                ? SignalPreporuke.Popularnost
                : SignalPreporuke.Ocjena;
        }

        return najjaciIzProfila;
    }

    /// <summary>Udio vrijednosti u profilu, ili null kad profil o tom signalu nema podatak.</summary>
    private static double? Udio(IReadOnlyDictionary<int, double> udjeli, int vrijednost)
    {
        if (udjeli.Count == 0)
        {
            return null;
        }

        return udjeli.TryGetValue(vrijednost, out var udio) ? udio : 0;
    }

    /// <summary>
    /// Koliko je dnevna tarifa blizu cijeni koju korisnik obicno trazi. Poklapanje
    /// pada linearno sa odstupanjem: dvostruko skuplje vozilo od uobicajenog nosi
    /// nulu, a ne negativan doprinos.
    /// </summary>
    private static double? BlizinaCijene(decimal? prosjecnaCijena, decimal dnevnaTarifa)
    {
        if (prosjecnaCijena is null || prosjecnaCijena.Value <= 0)
        {
            return null;
        }

        var odstupanje = (double)Math.Abs(dnevnaTarifa - prosjecnaCijena.Value)
                         / (double)prosjecnaCijena.Value;

        return Math.Clamp(1 - odstupanje, 0, 1);
    }
}
