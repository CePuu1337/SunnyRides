using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private List<Korisnik> _klijenti = new();
    private List<Korisnik> _osoblje = new();
    private Korisnik _administrator = null!;
    private Korisnik _uposlenik = null!;

    /// <summary>
    /// Kategorije koje pojedini klijent stvarno posjeduje. Koristi se kasnije, da se
    /// rezervacija ne dodijeli klijentu koji to vozilo ne smije voziti.
    /// </summary>
    private readonly Dictionary<int, HashSet<string>> _kategorijeKlijenta = new();

    private async Task SeedKorisniciAsync(CancellationToken ct)
    {
        // Jedan hash za sve seed naloge - lozinka je svuda "test".
        // Login provjerava istim algoritmom (BCrypt.Verify), pa se formati poklapaju.
        var lozinkaHash = BCrypt.Net.BCrypt.HashPassword("test");

        Korisnik Napravi(string korisnickoIme, string ime, string prezime, string email,
                         string telefon, int godine, Role uloga, bool blokiran = false)
        {
            var korisnik = new Korisnik
            {
                KorisnickoIme = korisnickoIme,
                Ime = ime,
                Prezime = prezime,
                Email = email,
                Telefon = telefon,
                DatumRodjenja = _danas.AddYears(-godine).AddDays(-Broj(0, 365)),
                LozinkaHash = lozinkaHash,
                Aktivan = true,
                Blokiran = blokiran,
                DatumRegistracije = _danas.AddDays(-Broj(30, 400))
            };
            korisnik.KorisnikRole.Add(new KorisnikRole { Role = uloga, DatumDodjele = korisnik.DatumRegistracije });
            _context.Korisnici.Add(korisnik);
            return korisnik;
        }

        // --- osoblje ---
        // Korisnicka imena "desktop", "mobile", "administrator" i "uposlenik" trazi
        // uputstvo (sekcija 5) za pristup pri pregledu rada.
        _administrator = Napravi("administrator", "Ammar", "Puce", "administrator@sunnyrides.ba", "+387 61 100 100", 28, _roleAdministrator);
        var desktop = Napravi("desktop", "Desktop", "Pregled", "desktop@sunnyrides.ba", "+387 61 100 101", 30, _roleAdministrator);
        _uposlenik = Napravi("uposlenik", "Emina", "Hodzic", "emina.hodzic@sunnyrides.ba", "+387 61 200 200", 26, _roleUposlenik);
        var uposlenikDrugi = Napravi("mirza.begic", "Mirza", "Begic", "mirza.begic@sunnyrides.ba", "+387 61 200 201", 34, _roleUposlenik);
        _osoblje = new List<Korisnik> { _administrator, desktop, _uposlenik, uposlenikDrugi };

        // --- klijenti ---
        var mobile = Napravi("mobile", "Mobilni", "Pregled", "mobile@sunnyrides.ba", "+387 62 300 300", 29, _roleKlijent);

        var podaciKlijenata = new (string ime, string prezime, int godine)[]
        {
            ("Amina",  "Selimovic", 24), ("Tarik",  "Music",     31), ("Lejla",  "Softic",   27),
            ("Haris",  "Dedic",     22), ("Nejra",  "Kovac",     35), ("Adnan",  "Zukic",    19),
            ("Sara",   "Halilovic", 26), ("Dino",   "Ramic",     41), ("Ajla",   "Bektas",   23),
            ("Kenan",  "Jusic",     29), ("Ilma",   "Cengic",    33), ("Faris",  "Alic",     20),
            ("Merjem", "Pasic",     38), ("Vedad",  "Coric",     25), ("Naida",  "Omerovic", 30)
        };

        _klijenti = new List<Korisnik> { mobile };
        var brojac = 1;
        foreach (var (ime, prezime, godine) in podaciKlijenata)
        {
            var korisnickoIme = $"{ime.ToLowerInvariant()}.{prezime.ToLowerInvariant()}";
            var blokiran = brojac == 13; // jedan blokiran klijent, da se modul blokade ima na cemu vidjeti
            _klijenti.Add(Napravi(
                korisnickoIme, ime, prezime,
                $"{korisnickoIme}@gmail.com",
                $"+387 6{Broj(1, 6)} {Broj(100, 999)} {Broj(100, 999)}",
                godine, _roleKlijent, blokiran));
            brojac++;
        }

        await _context.SaveChangesAsync(ct);

        // --- vozacke dozvole ---
        var kljucFotografije = await NapraviPlaceholderDozvoleAsync(ct);

        var a1 = _kategorije.Single(x => x.Oznaka == "A1");
        var a = _kategorije.Single(x => x.Oznaka == "A");
        var b = _kategorije.Single(x => x.Oznaka == "B");

        // Raspored je namjeran: vecina ima samo A1, manji dio A, dio B.
        // Zbog toga se u pretrazi stvarno vidi razlika kad se klijent promijeni.
        var raspored = new List<KategorijaDozvole[]>
        {
            new[] { a, b },            // mobile - vidi cijelu flotu, radi lakseg pregleda rada
            new[] { a1 }, new[] { a1 }, new[] { a1, b }, new[] { a },
            new[] { a1 }, new[] { b },  new[] { a, b },  new[] { a1 },
            new[] { a1, b }, new[] { a }, new[] { a1 },  new[] { b },
            new[] { a1 }, new[] { a, b }, new[] { a1 }
        };

        for (var i = 0; i < _klijenti.Count; i++)
        {
            var klijent = _klijenti[i];
            var kategorije = raspored[i];

            // Dvije dozvole cekaju verifikaciju, jedna je odbijena - da uposlenicki
            // modul za verifikaciju ima stvarnih predmeta za obradu.
            var status = i switch
            {
                5 => StatusDozvole.NaCekanju,
                11 => StatusDozvole.NaCekanju,
                14 => StatusDozvole.Odbijena,
                _ => StatusDozvole.Odobrena
            };

            var datumIzdavanja = _danas.AddYears(-Broj(1, 8));
            var dozvola = new VozackaDozvola
            {
                Korisnik = klijent,
                BrojDozvole = $"BA{Broj(100000, 999999)}{i:D2}",
                DatumIzdavanja = datumIzdavanja,
                DatumIsteka = datumIzdavanja.AddYears(10),
                PutanjaSlike = kljucFotografije,
                Status = status,
                DatumKreiranja = klijent.DatumRegistracije.AddDays(Broj(0, 5))
            };

            if (status == StatusDozvole.Odobrena)
            {
                dozvola.VerifikovaoKorisnik = _uposlenik;
                dozvola.DatumVerifikacije = dozvola.DatumKreiranja.AddHours(Broj(2, 48));
            }
            else if (status == StatusDozvole.Odbijena)
            {
                dozvola.VerifikovaoKorisnik = _uposlenik;
                dozvola.DatumVerifikacije = dozvola.DatumKreiranja.AddHours(Broj(2, 48));
                dozvola.RazlogOdbijanja = "Fotografija dozvole je nejasna - broj dozvole se ne moze procitati. Molimo posaljite novu fotografiju.";
            }

            foreach (var kategorija in kategorije)
            {
                dozvola.Kategorije.Add(new DozvolaKategorija { KategorijaDozvole = kategorija });
            }

            _context.VozackeDozvole.Add(dozvola);

            // Klijent smije rezervisati samo ako mu je dozvola odobrena.
            _kategorijeKlijenta[klijent.Id] = status == StatusDozvole.Odobrena
                ? kategorije.Select(x => x.Oznaka).ToHashSet()
                : new HashSet<string>();
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Pravi jednu placeholder fotografiju dozvole i vraca njen kljuc.
    ///
    /// Seed nema stvarne skenove vozackih dozvola i ne bi ih smio ni imati. Ali bez
    /// ijedne fotografije ekran za verifikaciju nema sta prikazati, pa se generise
    /// jedna neutralna slika koju dijele sve seed dozvole.
    ///
    /// Ide kroz istu pohranu kao i stvarni upload, dakle u privatni folder - da se
    /// ni u seedu ne uvede izuzetak od pravila da osjetljivi fajlovi nisu javni.
    /// </summary>
    private async Task<string?> NapraviPlaceholderDozvoleAsync(CancellationToken ct)
    {
        if (_pohrana is null)
        {
            return null;
        }

        using var slika = new Image<Rgba32>(640, 400, Color.ParseHex("D8DEE7"));

        using var bafer = new MemoryStream();
        await slika.SaveAsJpegAsync(bafer, new JpegEncoder { Quality = 80 }, ct);
        bafer.Position = 0;

        return await _pohrana.SacuvajPrivatnoAsync(bafer, bafer.Length, "dozvole/seed", ct);
    }
}
