using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private async Task SeedRecenzijeAsync(List<Rezervacija> rezervacije, CancellationToken ct)
    {
        var pohvale = new[]
        {
            "Vozilo je bilo besprijekorno cisto i ispravno. Preuzimanje je trajalo pet minuta.",
            "Sve po dogovoru, osoblje ljubazno. Skuter je savrsen za gradsku voznju.",
            "Odlicno iskustvo, preporucujem svakome ko obilazi obalu.",
            "Vozilo u odlicnom stanju, potrosnja manja nego sto sam ocekivao.",
            "Brzo i jednostavno. Kaciga i kofer su bili ukljuceni bez problema.",
            "Najam je prosao bez ijedne komplikacije, vratio bih se opet.",
            "Vrlo zadovoljan, jedino je red pri preuzimanju bio malo duzi."
        };

        var zamjerke = new[]
        {
            "Vozilo je bilo ispravno, ali nije bilo oprano izvana.",
            "Sve u redu, osim sto rezervoar nije bio pun pri preuzimanju.",
            "Solidno, ali bih volio da postoji opcija preuzimanja izvan radnog vremena.",
            "Dobro vozilo, malo skuplje nego kod konkurencije."
        };

        var zavrsene = rezervacije.Where(r => r.Status == StatusRezervacije.Completed).ToList();

        foreach (var rezervacija in zavrsene)
        {
            if (!Sansa(78))
            {
                continue;
            }

            var ocjena = Sansa(72) ? Broj(4, 6) : Broj(2, 4);
            var komentar = ocjena >= 4 ? Izaberi(pohvale) : Izaberi(zamjerke);

            _context.Recenzije.Add(new Recenzija
            {
                Korisnik = rezervacija.Korisnik,
                Vozilo = rezervacija.Vozilo,
                Rezervacija = rezervacija,
                Ocjena = ocjena,
                Komentar = komentar,
                DatumKreiranja = rezervacija.DatumDo.AddDays(Broj(1, 8)),
                // Nekoliko recenzija je skriveno moderacijom. Skrivena recenzija ne
                // ulazi ni u prosjecnu ocjenu ni u sistem preporuke.
                Skrivena = Sansa(5)
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedObavijestiINotifikacijeAsync(List<Rezervacija> rezervacije, CancellationToken ct)
    {
        // --- javne obavijesti agencije ---
        var obavijesti = new (string naslov, string tekst, int prijeDana)[]
        {
            ("Ljetna sezona je pocela",
             "Od 1. juna nasa flota radi punim kapacitetom u sve cetiri poslovnice. Preporucujemo rezervaciju nekoliko dana unaprijed jer su termini vikendom brzo popunjeni.", 45),
            ("Nova vozila u floti",
             "U Split i Dubrovnik stigli su novi Yamaha NMAX 125 skuteri. Idealni su za obilazak uzeg gradskog jezgra i obalne ceste.", 30),
            ("Elektricni skuteri sada dostupni",
             "NIU NQi GT je od ove sezone dostupan u Neumu i Mostaru. Domet je oko 70 km po punjenju, a punjac je ukljucen u cijenu najma.", 21),
            ("Radno vrijeme tokom praznika",
             "Tokom drzavnih praznika poslovnice rade skraceno, od 09:00 do 15:00. Preuzimanja izvan tog termina dogovaraju se unaprijed telefonom.", 10),
            ("Popust za duze najmove",
             "Za najam od sedam i vise dana automatski se primjenjuje popust od 10 posto. Popust se obracunava pri kreiranju rezervacije i vidljiv je u razradi cijene.", 4)
        };

        foreach (var (naslov, tekst, prijeDana) in obavijesti)
        {
            _context.Obavijesti.Add(new Obavijest
            {
                Naslov = naslov,
                Tekst = tekst,
                PutanjaSlike = null,
                DatumObjave = _danas.AddDays(-prijeDana).AddHours(9),
                Aktivna = true
            });
        }

        // --- notifikacije ---
        // Postoje za sve relevantne dogadjaje, ne samo za jedan.
        foreach (var rezervacija in rezervacije)
        {
            void Notifikacija(TipNotifikacije tip, string naslov, string tekst, DateTime kada)
            {
                _context.Notifikacije.Add(new Entities.Notifikacija
                {
                    Korisnik = rezervacija.Korisnik,
                    Rezervacija = rezervacija,
                    Naslov = naslov,
                    Tekst = tekst,
                    Tip = tip,
                    Procitana = kada < _danas.AddDays(-3) || Sansa(55),
                    DatumKreiranja = kada
                });
            }

            Notifikacija(TipNotifikacije.RezervacijaKreirana,
                "Rezervacija je kreirana",
                $"Rezervacija {rezervacija.Broj} je kreirana. Molimo dovrsite placanje u narednih 15 minuta.",
                rezervacija.DatumKreiranja);

            if (rezervacija.IsPaid)
            {
                Notifikacija(TipNotifikacije.PlacanjeUspjesno,
                    "Placanje je uspjesno",
                    $"Primili smo uplatu za rezervaciju {rezervacija.Broj}. Iznos: {rezervacija.UkupanIznos:0.00} EUR.",
                    rezervacija.DatumKreiranja.AddMinutes(Broj(3, 15)));
            }

            switch (rezervacija.Status)
            {
                case StatusRezervacije.Confirmed:
                    Notifikacija(TipNotifikacije.RezervacijaPotvrdjena,
                        "Rezervacija je potvrdjena",
                        $"Vase vozilo ceka vas {rezervacija.DatumOd:dd.MM.yyyy.} u {rezervacija.DatumOd:HH:mm} u poslovnici {rezervacija.Poslovnica.Naziv}.",
                        rezervacija.DatumKreiranja.AddMinutes(Broj(4, 18)));

                    if (rezervacija.DatumOd > _danas && rezervacija.DatumOd < _danas.AddDays(1))
                    {
                        Notifikacija(TipNotifikacije.PodsjetnikPreuzimanje,
                            "Podsjetnik za preuzimanje",
                            $"Preuzimanje vozila je sutra u {rezervacija.DatumOd:HH:mm}. Ponesite vozacku dozvolu i licnu kartu.",
                            rezervacija.DatumOd.AddDays(-1));
                    }
                    break;

                case StatusRezervacije.Completed:
                    Notifikacija(TipNotifikacije.VoziloVraceno,
                        "Najam je zavrsen",
                        $"Hvala na povjerenju. Rezervacija {rezervacija.Broj} je zavrsena, a depozit je vracen na vasu karticu.",
                        rezervacija.DatumDo.AddHours(Broj(1, 8)));
                    break;

                case StatusRezervacije.Cancelled:
                    Notifikacija(TipNotifikacije.RezervacijaOtkazana,
                        "Rezervacija je otkazana",
                        $"Rezervacija {rezervacija.Broj} je otkazana. Razlog: {rezervacija.RazlogOtkazivanja}",
                        rezervacija.DatumOtkazivanja ?? rezervacija.DatumKreiranja);

                    if (rezervacija.IsPaid)
                    {
                        Notifikacija(TipNotifikacije.PovratIzvrsen,
                            "Povrat sredstava je izvrsen",
                            "Sredstva su vracena na karticu kojom je placeno. Uplata je vidljiva u roku od 5 do 10 radnih dana.",
                            (rezervacija.DatumOtkazivanja ?? rezervacija.DatumKreiranja).AddHours(Broj(1, 12)));
                    }
                    break;
            }
        }

        // --- notifikacije o verifikaciji dozvola ---
        var dozvole = await _context.VozackeDozvole.ToListAsync(ct);
        foreach (var dozvola in dozvole.Where(d => d.Status != StatusDozvole.NaCekanju))
        {
            var odobrena = dozvola.Status == StatusDozvole.Odobrena;
            _context.Notifikacije.Add(new Entities.Notifikacija
            {
                KorisnikId = dozvola.KorisnikId,
                Naslov = odobrena ? "Vozacka dozvola je odobrena" : "Vozacka dozvola je odbijena",
                Tekst = odobrena
                    ? "Vasa vozacka dozvola je verifikovana. Sada mozete rezervisati vozila kategorija koje dozvola pokriva."
                    : $"Vasa vozacka dozvola nije prihvacena. Razlog: {dozvola.RazlogOdbijanja}",
                Tip = odobrena ? TipNotifikacije.DozvolaOdobrena : TipNotifikacije.DozvolaOdbijena,
                Procitana = Sansa(70),
                DatumKreiranja = dozvola.DatumVerifikacije ?? dozvola.DatumKreiranja
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Historija pretraga je ulazni podatak za sistem preporuke. Bez ovih zapisa
    /// recommender nema iz cega graditi profil korisnika.
    /// </summary>
    private async Task SeedHistorijuPretragaAsync(CancellationToken ct)
    {
        var aktivniKlijenti = _klijenti.Where(k => _kategorijeKlijenta[k.Id].Count > 0).ToList();

        for (var i = 0; i < 140; i++)
        {
            var klijent = Izaberi(aktivniKlijenti);
            var cijenaOd = Sansa(70) ? (decimal?)(Broj(2, 7) * 10) : null;
            var cijenaDo = cijenaOd.HasValue ? cijenaOd + Broj(3, 9) * 10 : null;

            _context.HistorijaPretraga.Add(new HistorijaPretrage
            {
                Korisnik = klijent,
                TipVozila = Sansa(80) ? Izaberi(_tipoviVozila) : null,
                Grad = Sansa(65) ? Izaberi(_gradovi) : null,
                Marka = Sansa(45) ? Izaberi(_marke) : null,
                CijenaOd = cijenaOd,
                CijenaDo = cijenaDo,
                DatumVrijeme = _danas.AddDays(-Broj(0, 180)).AddHours(Broj(7, 23)).AddMinutes(Broj(0, 60))
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
