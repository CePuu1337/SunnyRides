using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Rezervacije;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private async Task SeedPlacanjaIPrimopredajeAsync(List<Rezervacija> rezervacije, CancellationToken ct)
    {
        // Fotografije primopredaje su privatne, kao i fotografije dozvola, pa seed za
        // njih pravi placeholder u privatnom folderu. Sve seed primopredaje dijele isti fajl.
        var fotografija = await NapraviPlaceholderAsync("primopredaje/seed", "C9D3C4", ct);

        foreach (var rezervacija in rezervacije)
        {
            Placanje? placanje = null;

            // --- placanje ---
            if (rezervacija.IsPaid)
            {
                var placeno = rezervacija.DatumKreiranja.AddMinutes(Broj(2, 14));
                placanje = new Placanje
                {
                    Rezervacija = rezervacija,
                    Iznos = rezervacija.UkupanIznos,
                    Valuta = "EUR",
                    Status = StatusPlacanja.Succeeded,
                    Provider = "Stripe",
                    ProviderPaymentIntentId = $"pi_seed_{rezervacija.Broj.Replace("-", "").ToLowerInvariant()}",
                    // Osnova za svaki kasniji povrat je stvarno naplaceni iznos,
                    // nikad ponovni obracun iz cjenovnika.
                    NaplaceniIznos = rezervacija.UkupanIznos,
                    IdempotencyKey = $"rez-{rezervacija.Id}-v1",
                    DatumKreiranja = rezervacija.DatumKreiranja,
                    DatumAzuriranja = placeno
                };
                _context.Placanja.Add(placanje);

                if (rezervacija.Status == StatusRezervacije.Cancelled)
                {
                    DodajPovratZbogOtkazivanja(rezervacija, placanje);
                }
            }
            else if (rezervacija.Status == StatusRezervacije.Cancelled && Sansa(40))
            {
                // Neuspio pokusaj naplate prije nego je drzanje isteklo.
                _context.Placanja.Add(new Placanje
                {
                    Rezervacija = rezervacija,
                    Iznos = rezervacija.UkupanIznos,
                    Valuta = "EUR",
                    Status = StatusPlacanja.Failed,
                    Provider = "Stripe",
                    ProviderPaymentIntentId = $"pi_seed_fail_{rezervacija.Id}",
                    IdempotencyKey = $"rez-{rezervacija.Id}-v1",
                    DatumKreiranja = rezervacija.DatumKreiranja,
                    DatumAzuriranja = rezervacija.DatumKreiranja.AddMinutes(Broj(1, 12))
                });
            }
            else if (rezervacija.Status == StatusRezervacije.Pending)
            {
                // Otvoren intent - pri ponovnom pokusaju placanja koristi se ovaj,
                // a ne novi.
                _context.Placanja.Add(new Placanje
                {
                    Rezervacija = rezervacija,
                    Iznos = rezervacija.UkupanIznos,
                    Valuta = "EUR",
                    Status = StatusPlacanja.Created,
                    Provider = "Stripe",
                    ProviderPaymentIntentId = $"pi_seed_open_{rezervacija.Id}",
                    IdempotencyKey = $"rez-{rezervacija.Id}-v1",
                    DatumKreiranja = rezervacija.DatumKreiranja
                });
            }

            // --- primopredaja ---
            if (rezervacija.Status == StatusRezervacije.Completed)
            {
                var iznosStete = DodajPrimopredaju(rezervacija, fotografija);

                // Obracun depozita: uplaceno - steta. Ostatak se vraca kao zaseban
                // zapis o povratu; otkazivanje i povrat depozita nisu isti dogadjaj.
                if (placanje is not null)
                {
                    var ostatakDepozita = Zaokruzi(rezervacija.IznosDepozita - iznosStete);
                    if (ostatakDepozita < 0)
                    {
                        ostatakDepozita = 0;
                    }

                    _context.Refundi.Add(new Refund
                    {
                        Placanje = placanje,
                        Iznos = ostatakDepozita,
                        Razlog = iznosStete > 0
                            ? $"Povrat depozita umanjen za evidentiranu stetu od {iznosStete:0.00} EUR."
                            : "Povrat depozita nakon urednog vracanja vozila.",
                        Status = StatusPlacanja.Succeeded,
                        ProviderRefundId = $"re_seed_dep_{rezervacija.Id}",
                        KreiraoKorisnik = _uposlenik,
                        DatumKreiranja = rezervacija.DatumDo.AddHours(Broj(1, 26))
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Povrat po politici otkazivanja.
    ///
    /// Racuna ga <c>PravilaOtkazivanja</c> - isto pravilo koje servis primjenjuje na
    /// zivim podacima. Da je ovdje prepisano, seed bi poslije prve izmjene pravila
    /// poceo pricati drugu pricu od aplikacije, a to je neslaganje koje se primijeti
    /// tek kad neko uporedi stari i novi zapis.
    /// </summary>
    private void DodajPovratZbogOtkazivanja(Rezervacija rezervacija, Placanje placanje)
    {
        var otkazano = rezervacija.DatumOtkazivanja ?? rezervacija.DatumKreiranja;
        var otkazalaAgencija = rezervacija.OtkazaoKorisnik is not null
                               && _osoblje.Any(o => o.Id == rezervacija.OtkazaoKorisnik.Id);

        var obracun = PravilaOtkazivanja.Izracunaj(new UlazOtkazivanja
        {
            DatumOd = rezervacija.DatumOd,
            Sada = otkazano,
            OtkazujeAgencija = otkazalaAgencija,

            // Osnova je stvarno naplaceni iznos, ne ukupan iznos rezervacije.
            Naplaceno = placanje.NaplaceniIznos ?? 0m,
            IznosDepozita = rezervacija.IznosDepozita
        });

        _context.Refundi.Add(new Refund
        {
            Placanje = placanje,
            Iznos = obracun.UkupanPovrat,
            Razlog = obracun.Obrazlozenje,
            Status = StatusPlacanja.Succeeded,
            ProviderRefundId = $"re_seed_{rezervacija.Id}",
            KreiraoKorisnik = rezervacija.OtkazaoKorisnik,
            DatumKreiranja = otkazano.AddMinutes(Broj(1, 30))
        });
    }

    /// <summary>Vraca iznos evidentirane stete, ili nulu ako je stete nema.</summary>
    private decimal DodajPrimopredaju(Rezervacija rezervacija, string? fotografija)
    {
        var kmPriIzdavanju = rezervacija.Vozilo.Kilometraza - Broj(200, 3000);
        if (kmPriIzdavanju < 0)
        {
            kmPriIzdavanju = 0;
        }

        var izdavanje = new Primopredaja
        {
            Rezervacija = rezervacija,
            Tip = TipPrimopredaje.Izdavanje,
            DatumVrijeme = rezervacija.DatumOd.AddMinutes(Broj(0, 40)),
            Kilometraza = kmPriIzdavanju,
            NivoGoriva = Broj(80, 101),
            Napomena = "Vozilo izdato u ispravnom stanju.",
            KontrolnaListaProdjena = true,
            IzvrsioKorisnik = _uposlenik
        };
        DodajFotografiju(izdavanje, fotografija);
        _context.Primopredaje.Add(izdavanje);

        var predjeno = Broj(40, 900);
        var kasnjenjeMinuta = Sansa(25) ? Broj(5, 240) : Broj(0, 45);

        var povrat = new Primopredaja
        {
            Rezervacija = rezervacija,
            Tip = TipPrimopredaje.Povrat,
            DatumVrijeme = rezervacija.DatumDo.AddMinutes(kasnjenjeMinuta),
            Kilometraza = kmPriIzdavanju + predjeno,
            NivoGoriva = Broj(20, 101),
            KontrolnaListaProdjena = true,
            IzvrsioKorisnik = _uposlenik
        };
        DodajFotografiju(povrat, fotografija);

        // Oko svakog sedmog povrata ima evidentirano ostecenje. Kad ga ima, opis i
        // fotografija su obavezni, a iznos umanjuje povrat depozita.
        decimal iznosStete = 0m;
        if (Sansa(15))
        {
            var opisi = new[]
            {
                "Ogrebotina na desnoj bocnoj masci, duzine oko 8 cm.",
                "Napuknuto retrovizorsko staklo na lijevoj strani.",
                "Ostecen prednji blatobran, potrebna zamjena.",
                "Probusena zadnja guma, zamijenjena o trosku najmodavca."
            };
            iznosStete = Zaokruzi(Broj(20, 160));

            povrat.EvidencijaStete = new EvidencijaStete
            {
                Opis = Izaberi(opisi),
                Iznos = iznosStete,
                DatumEvidentiranja = povrat.DatumVrijeme,
                EvidentiraoKorisnik = _uposlenik
            };
            povrat.Napomena = "Evidentirano ostecenje pri povratu, vidjeti prilozene fotografije.";
        }
        else
        {
            povrat.Napomena = "Vozilo vraceno bez ostecenja.";
        }

        _context.Primopredaje.Add(povrat);
        return iznosStete;
    }

    private static void DodajFotografiju(Primopredaja primopredaja, string? kljuc)
    {
        if (kljuc is not null)
        {
            primopredaja.Fotografije.Add(new FotografijaPrimopredaje { Putanja = kljuc });
        }
    }
}
