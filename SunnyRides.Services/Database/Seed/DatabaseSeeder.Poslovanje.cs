using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private async Task SeedPoslovanjeAsync(CancellationToken ct)
    {
        var rezervacije = new List<Rezervacija>();
        var iskoristeniTrojci = new HashSet<(int korisnik, int vozilo, DateTime od)>();
        var redniBroj = 1;

        var horizont = _danas.AddDays(60);
        var pocetakHistorije = _danas.AddDays(-190);

        // Gornja granica po vozilu postoji samo da jedno vozilo ne popuni cijeli
        // kalendar ako mu razmaci ispadnu kratki. Sama petlja ide dok ne dodje do
        // horizonta, a ne fiksan broj puta - inace se rezervacije nagomilaju na
        // pocetku perioda i nikad ne stignu do danasnjeg dana.
        const int maksimalnoPoVozilu = 14;

        foreach (var vozilo in _vozila.Where(v => v.Aktivno))
        {
            var kursor = pocetakHistorije.AddDays(Broj(0, 20));
            var napravljeno = 0;

            while (kursor < horizont && napravljeno < maksimalnoPoVozilu)
            {
                kursor = kursor.AddDays(Broj(3, 26));
                var trajanjeDana = Broj(1, 11);

                var datumOd = kursor.AddHours(Broj(8, 18));
                var datumDo = datumOd.AddDays(trajanjeDana);
                if (datumDo > horizont)
                {
                    break;
                }

                var klijent = IzaberiKlijentaZa(vozilo);
                if (klijent is null)
                {
                    break;
                }

                if (!iskoristeniTrojci.Add((klijent.Id, vozilo.Id, datumOd)))
                {
                    continue;
                }

                var rezervacija = NapraviRezervaciju(vozilo, klijent, datumOd, datumDo, trajanjeDana, redniBroj++);
                rezervacije.Add(rezervacija);
                _context.Rezervacije.Add(rezervacija);

                napravljeno++;
                kursor = datumDo.Date.AddDays(1);
            }
        }

        await _context.SaveChangesAsync(ct);

        await SeedPlacanjaIPrimopredajeAsync(rezervacije, ct);
        await SeedRecenzijeAsync(rezervacije, ct);
        await SeedObavijestiINotifikacijeAsync(rezervacije, ct);
        await SeedHistorijuPretragaAsync(ct);
    }

    /// <summary>
    /// Bira klijenta koji stvarno smije voziti dato vozilo. Kategorija A pokriva i A1,
    /// sto ovdje nije posebno pravilo nego posljedica toga da A nema ogranicenja.
    /// </summary>
    private Korisnik? IzaberiKlijentaZa(Vozilo vozilo)
    {
        var potrebna = vozilo.ModelVozila.KategorijaDozvole.Oznaka;

        var podobni = _klijenti.Where(k =>
        {
            if (k.Blokiran)
            {
                return false;
            }

            var kategorije = _kategorijeKlijenta[k.Id];
            if (kategorije.Count == 0)
            {
                return false;
            }

            return kategorije.Contains(potrebna)
                || (potrebna == "A1" && kategorije.Contains("A"));
        }).ToList();

        return podobni.Count == 0 ? null : Izaberi(podobni);
    }

    private Rezervacija NapraviRezervaciju(Vozilo vozilo, Korisnik klijent,
        DateTime datumOd, DateTime datumDo, int dana, int redniBroj)
    {
        // Status se izvodi iz odnosa termina prema danasnjem danu.
        var status = datumDo < _danas
            ? (Sansa(80) ? StatusRezervacije.Completed : StatusRezervacije.Cancelled)
            : datumOd > _danas
                ? (Sansa(70) ? StatusRezervacije.Confirmed
                    : Sansa(50) ? StatusRezervacije.Cancelled : StatusRezervacije.Pending)
                : StatusRezervacije.Confirmed;

        var paket = Sansa(75) ? Izaberi(_paketiOsiguranja) : null;
        var datumKreiranja = datumOd.AddDays(-Broj(1, 21));

        // Rezervacija ne moze nastati prije nego je napravljena. Kod buducih termina
        // oduzimanje od datuma preuzimanja zna zavrsiti u buducnosti - termin za tri
        // sedmice minus deset dana je i dalje sutra - pa bi cijela historija statusa
        // nosila datume koji jos nisu nastupili. Tada se datum pomjera unazad od
        // danasnjeg dana.
        if (datumKreiranja > _danas)
        {
            datumKreiranja = _danas.AddDays(-Broj(0, 4)).AddHours(-Broj(1, 20));
        }

        // Dio otkazanih rezervacija je prije otkazivanja bio placen - njima kasnije
        // nastaje i zapis o povratu novca. Ostale su otkazane jos u statusu Pending.
        var bioPlacen = status switch
        {
            StatusRezervacije.Confirmed or StatusRezervacije.Completed => true,
            StatusRezervacije.Cancelled => Sansa(55),
            _ => false
        };

        var rezervacija = new Rezervacija
        {
            Broj = $"SR-{datumOd:yyyy}-{redniBroj:D5}",
            Korisnik = klijent,
            Vozilo = vozilo,
            Poslovnica = vozilo.Poslovnica,
            PaketOsiguranja = paket,
            DatumOd = datumOd,
            DatumDo = datumDo,
            Status = status,
            IznosDepozita = vozilo.IznosDepozita,
            DatumKreiranja = datumKreiranja,
            IsPaid = bioPlacen
        };

        // --- stavke opreme ---
        var brojStavki = Broj(0, 4);
        var izabraneVrste = _vrsteOpreme.OrderBy(_ => _rnd.Next()).Take(brojStavki).ToList();
        decimal iznosOpreme = 0;

        foreach (var vrsta in izabraneVrste)
        {
            var kolicina = Broj(1, 3);
            var cijenaPoJedinici = vrsta.CijenaPoDanu ?? vrsta.FiksnaCijena ?? 0m;
            var iznos = vrsta.CijenaPoDanu.HasValue
                ? Zaokruzi(cijenaPoJedinici * kolicina * dana)
                : Zaokruzi(cijenaPoJedinici * kolicina);

            rezervacija.StavkeOpreme.Add(new StavkaOpreme
            {
                VrstaOpreme = vrsta,
                Kolicina = kolicina,
                // Cijena se snima u trenutku kreiranja, da kasnija izmjena cjenovnika
                // ne promijeni historijsku rezervaciju.
                CijenaPoJedinici = cijenaPoJedinici,
                Iznos = iznos
            });

            iznosOpreme += iznos;
        }

        // --- iznos ---
        // Ovo je historijski iznos, onakav kakav je bio naplacen u trenutku kreiranja.
        // Pravila su ista kao u PricingService, koji je pri radu aplikacije jedini
        // mjerodavan; ovdje se samo upisuju vjerodostojni podaci iz proslosti.
        var mnozilac = datumOd.Month is >= 6 and <= 9 ? 1.30m : 0.85m;
        var osnovica = Zaokruzi(vozilo.DnevnaTarifa * dana * mnozilac);
        var procenatPopusta = dana >= 7 ? 10m : dana >= 3 ? 5m : 0m;
        var popust = Zaokruzi(osnovica * procenatPopusta / 100m);
        var osiguranje = paket is null ? 0m : Zaokruzi(paket.CijenaPoDanu * dana);

        rezervacija.IznosPopusta = popust;
        rezervacija.UkupanIznos = Zaokruzi(osnovica - popust + iznosOpreme + osiguranje + vozilo.IznosDepozita);

        // --- drzanje vozila i otkazivanje ---
        if (status == StatusRezervacije.Pending)
        {
            // Dio neplacenih rezervacija ima isteklo drzanje - to su tacno oni zapisi
            // koje periodicni posao u workeru treba prebaciti u Cancelled.
            rezervacija.DrziDo = Sansa(50)
                ? DateTime.UtcNow.AddMinutes(15)
                : DateTime.UtcNow.AddMinutes(-Broj(20, 600));
        }

        if (status == StatusRezervacije.Cancelled)
        {
            var razlozi = new[]
            {
                "Klijent je odustao od najma.",
                "Promjena planova putovanja.",
                "Vozilo je otislo na neplanirani servis - ponudjena zamjena odbijena.",
                "Isteklo vrijeme za placanje.",
                "Klijent nije dostavio vazecu vozacku dozvolu."
            };
            rezervacija.RazlogOtkazivanja = Izaberi(razlozi);
            rezervacija.DatumOtkazivanja = datumKreiranja.AddDays(Broj(1, 10));
            rezervacija.OtkazaoKorisnik = Sansa(60) ? klijent : _uposlenik;
        }

        DodajHistorijuStatusa(rezervacija, klijent);
        return rezervacija;
    }

    /// <summary>Audit trag prelaza: ko, kada, razlog i opis - za svaki prelaz jedan zapis.</summary>
    private void DodajHistorijuStatusa(Rezervacija rezervacija, Korisnik klijent)
    {
        void Zapis(StatusRezervacije? iz, StatusRezervacije u, string opis, DateTime kada, Korisnik? ko, string? razlog = null)
        {
            rezervacija.HistorijaStatusa.Add(new HistorijaStatusaRezervacije
            {
                StatusIz = iz,
                StatusU = u,
                Opis = opis,
                Razlog = razlog,
                IzvrsioKorisnik = ko,
                DatumVrijeme = kada
            });
        }

        Zapis(null, StatusRezervacije.Pending, "Rezervacija kreirana, ceka se placanje.",
            rezervacija.DatumKreiranja, klijent);

        switch (rezervacija.Status)
        {
            case StatusRezervacije.Confirmed:
            case StatusRezervacije.Completed:
                var placeno = rezervacija.DatumKreiranja.AddMinutes(Broj(2, 14));
                Zapis(StatusRezervacije.Pending, StatusRezervacije.Confirmed,
                    "Placanje verifikovano na serveru.", placeno, klijent);

                if (rezervacija.Status == StatusRezervacije.Completed)
                {
                    Zapis(StatusRezervacije.Confirmed, StatusRezervacije.Completed,
                        "Evidentiran povrat vozila.", rezervacija.DatumDo.AddMinutes(Broj(5, 90)), _uposlenik);
                }
                break;

            case StatusRezervacije.Cancelled:
                var izStatusa = rezervacija.IsPaid ? StatusRezervacije.Confirmed : StatusRezervacije.Pending;
                Zapis(izStatusa, StatusRezervacije.Cancelled, "Rezervacija otkazana.",
                    rezervacija.DatumOtkazivanja ?? rezervacija.DatumKreiranja,
                    rezervacija.OtkazaoKorisnik, rezervacija.RazlogOtkazivanja);
                break;
        }
    }
}
