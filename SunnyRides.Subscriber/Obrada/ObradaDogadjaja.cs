using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Model.Poruke;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Subscriber.Email;

namespace SunnyRides.Subscriber.Obrada;

/// <summary>
/// Sve sto worker radi kad stigne poruka: procita zapis iz baze, posalje email i
/// upise notifikaciju.
///
/// Poruka nosi samo identifikator, pa se podaci citaju sada, u trenutku obrade, a ne
/// onakvi kakvi su bili u trenutku slanja. Ako se u medjuvremenu nesto promijenilo,
/// klijent dobija tacno stanje.
/// </summary>
public class ObradaDogadjaja
{
    private readonly SunnyRidesDbContext _context;
    private readonly IPosiljalacEmaila _email;
    private readonly ILogger<ObradaDogadjaja> _logger;

    public ObradaDogadjaja(
        SunnyRidesDbContext context, IPosiljalacEmaila email, ILogger<ObradaDogadjaja> logger)
    {
        _context = context;
        _email = email;
        _logger = logger;
    }

    public async Task ObradiAsync(string red, string tijelo, CancellationToken ct)
    {
        switch (red)
        {
            case Redovi.RezervacijaKreirana:
                await RezervacijaKreiranaAsync(Procitaj<RezervacijaPoruka>(tijelo).RezervacijaId, ct);
                break;

            case Redovi.PlacanjeUspjesno:
                await PlacanjeUspjesnoAsync(Procitaj<PlacanjePoruka>(tijelo), ct);
                break;

            case Redovi.RezervacijaOtkazana:
                await RezervacijaOtkazanaAsync(Procitaj<RezervacijaPoruka>(tijelo).RezervacijaId, ct);
                break;

            case Redovi.PovratIzvrsen:
                await PovratIzvrsenAsync(Procitaj<PovratPoruka>(tijelo), ct);
                break;

            case Redovi.DozvolaVerifikovana:
                await DozvolaVerifikovanaAsync(Procitaj<DozvolaPoruka>(tijelo).DozvolaId, ct);
                break;

            case Redovi.PodsjetnikPreuzimanje:
                await PodsjetnikAsync(Procitaj<RezervacijaPoruka>(tijelo).RezervacijaId, ct);
                break;

            case Redovi.ResetLozinke:
                await ResetLozinkeAsync(Procitaj<ResetLozinkePoruka>(tijelo), ct);
                break;

            default:
                _logger.LogWarning("Red {Red} nema obradu, poruka se preskace.", red);
                break;
        }
    }

    // --- rezervacije -------------------------------------------------------

    private async Task RezervacijaKreiranaAsync(int rezervacijaId, CancellationToken ct)
    {
        var rezervacija = await UcitajRezervacijuAsync(rezervacijaId, ct);
        if (rezervacija is null)
        {
            return;
        }

        var minuta = rezervacija.DrziDo is null
            ? 15
            : Math.Max(1, (int)(rezervacija.DrziDo.Value - DateTime.UtcNow).TotalMinutes);

        var tekst =
            $"Postovani/a {rezervacija.Korisnik.Ime},\n\n" +
            $"vasa rezervacija {rezervacija.Broj} je zaprimljena.\n\n" +
            OpisRezervacije(rezervacija) +
            $"\nIznos za placanje: {rezervacija.UkupanIznos:0.00} EUR (ukljucen depozit od {rezervacija.IznosDepozita:0.00} EUR).\n" +
            $"Vozilo vam je rezervisano jos {minuta} min. Placanje zavrsite u aplikaciji.\n\n" +
            "SunnyRides";

        await JaviAsync(rezervacija.Korisnik, rezervacija.Id, TipNotifikacije.RezervacijaKreirana,
            $"Rezervacija {rezervacija.Broj} ceka placanje",
            $"Zavrsite placanje u narednih {minuta} min da vam vozilo ostane rezervisano.", tekst, ct);
    }

    private async Task PlacanjeUspjesnoAsync(PlacanjePoruka poruka, CancellationToken ct)
    {
        var rezervacija = await UcitajRezervacijuAsync(poruka.RezervacijaId, ct);
        if (rezervacija is null)
        {
            return;
        }

        var placanje = await _context.Placanja
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == poruka.PlacanjeId, ct);

        var naplaceno = placanje?.NaplaceniIznos ?? rezervacija.UkupanIznos;

        var tekst =
            $"Postovani/a {rezervacija.Korisnik.Ime},\n\n" +
            $"placanje je potvrdjeno i rezervacija {rezervacija.Broj} je potvrdjena.\n\n" +
            OpisRezervacije(rezervacija) +
            $"\nNaplaceno: {naplaceno:0.00} EUR.\n" +
            "Pri preuzimanju ponesite vozacku dozvolu i licnu kartu.\n\n" +
            "SunnyRides";

        await JaviAsync(rezervacija.Korisnik, rezervacija.Id, TipNotifikacije.PlacanjeUspjesno,
            "Placanje je potvrdjeno",
            $"Rezervacija {rezervacija.Broj} je potvrdjena. Naplaceno {naplaceno:0.00} EUR.", tekst, ct);
    }

    private async Task RezervacijaOtkazanaAsync(int rezervacijaId, CancellationToken ct)
    {
        var rezervacija = await UcitajRezervacijuAsync(rezervacijaId, ct);
        if (rezervacija is null)
        {
            return;
        }

        var razlog = rezervacija.RazlogOtkazivanja?.Naziv ?? rezervacija.NapomenaOtkazivanja ?? "Nije naveden";

        var tekst =
            $"Postovani/a {rezervacija.Korisnik.Ime},\n\n" +
            $"rezervacija {rezervacija.Broj} je otkazana.\n\n" +
            OpisRezervacije(rezervacija) +
            $"\nRazlog: {razlog}\n" +
            (rezervacija.NapomenaOtkazivanja is null ? "" : $"Napomena: {rezervacija.NapomenaOtkazivanja}\n") +
            "Ako je rezervacija bila placena, o povratu sredstava dobijate zasebnu poruku.\n\n" +
            "SunnyRides";

        await JaviAsync(rezervacija.Korisnik, rezervacija.Id, TipNotifikacije.RezervacijaOtkazana,
            $"Rezervacija {rezervacija.Broj} je otkazana", $"Razlog: {razlog}", tekst, ct);
    }

    private async Task PodsjetnikAsync(int rezervacijaId, CancellationToken ct)
    {
        var rezervacija = await UcitajRezervacijuAsync(rezervacijaId, ct);
        if (rezervacija is null)
        {
            return;
        }

        var tekst =
            $"Postovani/a {rezervacija.Korisnik.Ime},\n\n" +
            $"podsjecamo vas na preuzimanje vozila po rezervaciji {rezervacija.Broj}.\n\n" +
            OpisRezervacije(rezervacija) +
            "\nPonesite vozacku dozvolu i licnu kartu.\n\n" +
            "SunnyRides";

        await JaviAsync(rezervacija.Korisnik, rezervacija.Id, TipNotifikacije.PodsjetnikPreuzimanje,
            "Podsjetnik za preuzimanje vozila",
            $"Preuzimanje je {rezervacija.DatumOd:dd.MM.yyyy. u HH:mm} (UTC), poslovnica {rezervacija.Poslovnica.Naziv}.",
            tekst, ct);
    }

    // --- novac -------------------------------------------------------------

    private async Task PovratIzvrsenAsync(PovratPoruka poruka, CancellationToken ct)
    {
        var povrat = await _context.Refundi
            .AsNoTracking()
            .Include(x => x.Placanje).ThenInclude(p => p.Rezervacija).ThenInclude(r => r.Korisnik)
            .FirstOrDefaultAsync(x => x.Id == poruka.PovratId, ct);

        if (povrat is null)
        {
            _logger.LogWarning("Povrat {PovratId} ne postoji, poruka se preskace.", poruka.PovratId);
            return;
        }

        var rezervacija = povrat.Placanje.Rezervacija;

        var tekst =
            $"Postovani/a {rezervacija.Korisnik.Ime},\n\n" +
            $"povrat sredstava za rezervaciju {rezervacija.Broj} je poslan.\n\n" +
            $"Iznos: {povrat.Iznos:0.00} EUR\n" +
            $"Razlog: {povrat.Razlog}\n\n" +
            "Sredstva se vracaju na karticu kojom je placeno, obicno u roku od 5 do 10 radnih dana.\n\n" +
            "SunnyRides";

        await JaviAsync(rezervacija.Korisnik, rezervacija.Id, TipNotifikacije.PovratIzvrsen,
            "Povrat sredstava je izvrsen",
            $"Vraceno {povrat.Iznos:0.00} EUR za rezervaciju {rezervacija.Broj}.", tekst, ct);
    }

    // --- dozvole -----------------------------------------------------------

    private async Task DozvolaVerifikovanaAsync(int dozvolaId, CancellationToken ct)
    {
        var dozvola = await _context.VozackeDozvole
            .AsNoTracking()
            .Include(x => x.Korisnik)
            .FirstOrDefaultAsync(x => x.Id == dozvolaId, ct);

        if (dozvola is null)
        {
            _logger.LogWarning("Dozvola {DozvolaId} ne postoji, poruka se preskace.", dozvolaId);
            return;
        }

        var odobrena = dozvola.Status == StatusDozvole.Odobrena;

        var tekst = odobrena
            ? $"Postovani/a {dozvola.Korisnik.Ime},\n\n" +
              "vasa vozacka dozvola je odobrena. Od sada mozete rezervisati vozila kategorija koje dozvola pokriva.\n\n" +
              "SunnyRides"
            : $"Postovani/a {dozvola.Korisnik.Ime},\n\n" +
              "vasa vozacka dozvola nije odobrena.\n\n" +
              $"Razlog: {dozvola.RazlogOdbijanja}\n\n" +
              "Mozete prijaviti ispravljene podatke i novu fotografiju u aplikaciji.\n\n" +
              "SunnyRides";

        await JaviAsync(dozvola.Korisnik, rezervacijaId: null,
            odobrena ? TipNotifikacije.DozvolaOdobrena : TipNotifikacije.DozvolaOdbijena,
            odobrena ? "Vozacka dozvola je odobrena" : "Vozacka dozvola nije odobrena",
            odobrena
                ? "Mozete rezervisati vozila kategorija koje vasa dozvola pokriva."
                : $"Razlog: {dozvola.RazlogOdbijanja}",
            tekst, ct);
    }

    // --- nalog -------------------------------------------------------------

    /// <summary>
    /// Kod ide iskljucivo u email. Notifikacija u aplikaciji samo kaze da je kod
    /// poslan - da ga ne bi vidio neko ko je vec na tudjem uredjaju otvorio spisak
    /// obavjestenja.
    /// </summary>
    private async Task ResetLozinkeAsync(ResetLozinkePoruka poruka, CancellationToken ct)
    {
        var korisnik = await _context.Korisnici
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == poruka.KorisnikId, ct);

        if (korisnik is null)
        {
            _logger.LogWarning("Korisnik {KorisnikId} ne postoji, poruka se preskace.", poruka.KorisnikId);
            return;
        }

        var minuta = Math.Max(1, (int)Math.Round((poruka.IsticeUtc - DateTime.UtcNow).TotalMinutes));

        var tekst =
            $"Postovani/a {korisnik.Ime},\n\n" +
            "zatrazena je promjena lozinke za vas SunnyRides nalog.\n\n" +
            $"Kod: {poruka.Kod}\n" +
            $"Kod vazi jos {minuta} min i moze se iskoristiti jednom.\n\n" +
            "Ako promjenu niste trazili vi, zanemarite ovu poruku - lozinka ostaje ista.\n\n" +
            "SunnyRides";

        await JaviAsync(korisnik, rezervacijaId: null, TipNotifikacije.ResetLozinke,
            "Kod za promjenu lozinke",
            "Kod za promjenu lozinke je poslan na vasu email adresu.", tekst, ct);
    }

    // --- zajednicko --------------------------------------------------------

    /// <summary>
    /// Notifikacija u bazi i email idu zajedno. Prvo se upisuje notifikacija: ona je
    /// trag da je dogadjaj obradjen, a email je stvar koja moze pasti zbog tudjeg
    /// servera.
    /// </summary>
    private async Task JaviAsync(
        Korisnik korisnik, int? rezervacijaId, TipNotifikacije tip,
        string naslov, string kratakTekst, string email, CancellationToken ct)
    {
        _context.Notifikacije.Add(new Notifikacija
        {
            KorisnikId = korisnik.Id,
            RezervacijaId = rezervacijaId,
            Naslov = naslov,
            Tekst = kratakTekst,
            Tip = tip,
            Procitana = false,
            DatumKreiranja = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        await _email.PosaljiAsync(korisnik.Email, naslov, email, ct);
    }

    private async Task<Rezervacija?> UcitajRezervacijuAsync(int rezervacijaId, CancellationToken ct)
    {
        var rezervacija = await _context.Rezervacije
            .AsNoTracking()
            .Include(x => x.Korisnik)
            .Include(x => x.Poslovnica)
            .Include(x => x.RazlogOtkazivanja)
            .Include(x => x.Vozilo).ThenInclude(v => v.ModelVozila).ThenInclude(m => m.Marka)
            .FirstOrDefaultAsync(x => x.Id == rezervacijaId, ct);

        if (rezervacija is null)
        {
            _logger.LogWarning("Rezervacija {RezervacijaId} ne postoji, poruka se preskace.", rezervacijaId);
        }

        return rezervacija;
    }

    private static string OpisRezervacije(Rezervacija rezervacija) =>
        $"Vozilo: {rezervacija.Vozilo.ModelVozila.Marka.Naziv} {rezervacija.Vozilo.ModelVozila.Naziv} " +
        $"({rezervacija.Vozilo.RegistarskaOznaka})\n" +
        $"Preuzimanje: {rezervacija.DatumOd:dd.MM.yyyy. HH:mm} (UTC), poslovnica {rezervacija.Poslovnica.Naziv}\n" +
        $"Vracanje: {rezervacija.DatumDo:dd.MM.yyyy. HH:mm} (UTC)\n";

    private static T Procitaj<T>(string tijelo) =>
        JsonSerializer.Deserialize<T>(tijelo)
        ?? throw new InvalidOperationException($"Poruka nije u ocekivanom obliku: {tijelo}");
}
