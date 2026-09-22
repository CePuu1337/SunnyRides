using SunnyRides.Model.Konstante;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private List<Poslovnica> _poslovnice = new();
    private List<ModelVozila> _modeli = new();
    private List<VrstaOpreme> _vrsteOpreme = new();
    private List<PaketOsiguranja> _paketiOsiguranja = new();
    private List<RazlogOtkazivanja> _razloziKlijenta = new();
    private List<RazlogOtkazivanja> _razloziAgencije = new();
    private List<KategorijaDozvole> _kategorije = new();
    private List<Marka> _marke = new();
    private List<TipVozila> _tipoviVozila = new();
    private List<Grad> _gradovi = new();
    private Role _roleAdministrator = null!;
    private Role _roleUposlenik = null!;
    private Role _roleKlijent = null!;

    /// <summary>Naziv seed slike po modelu vozila, koristi se i za vozila i za primopredaje.</summary>
    private readonly Dictionary<string, string> _slikaModela = new();

    private async Task SeedSifrarniciAsync(CancellationToken ct)
    {
        // --- uloge ---
        _roleAdministrator = new Role { Naziv = Uloge.Administrator, Opis = "Puni pristup svim modulima" };
        _roleUposlenik = new Role { Naziv = Uloge.Uposlenik, Opis = "Operativni rad: rezervacije, primopredaja, dozvole" };
        _roleKlijent = new Role { Naziv = Uloge.Klijent, Opis = "Klijent mobilne aplikacije" };
        _context.Role.AddRange(_roleAdministrator, _roleUposlenik, _roleKlijent);

        // --- drzave i gradovi ---
        var bih = new Drzava { Naziv = "Bosna i Hercegovina", Skracenica = "BIH" };
        var hrvatska = new Drzava { Naziv = "Hrvatska", Skracenica = "HRV" };
        var crnaGora = new Drzava { Naziv = "Crna Gora", Skracenica = "MNE" };
        _context.Drzave.AddRange(bih, hrvatska, crnaGora);

        var neum = new Grad { Drzava = bih, Naziv = "Neum", PostanskiBroj = "88390" };
        var mostar = new Grad { Drzava = bih, Naziv = "Mostar", PostanskiBroj = "88000" };
        var sarajevo = new Grad { Drzava = bih, Naziv = "Sarajevo", PostanskiBroj = "71000" };
        var split = new Grad { Drzava = hrvatska, Naziv = "Split", PostanskiBroj = "21000" };
        var dubrovnik = new Grad { Drzava = hrvatska, Naziv = "Dubrovnik", PostanskiBroj = "20000" };
        var budva = new Grad { Drzava = crnaGora, Naziv = "Budva", PostanskiBroj = "85310" };
        _gradovi = new List<Grad> { neum, mostar, sarajevo, split, dubrovnik, budva };
        _context.Gradovi.AddRange(_gradovi);

        // --- poslovnice ---
        _poslovnice = new List<Poslovnica>
        {
            new() { Grad = neum,      Naziv = "SunnyRides Neum",      Adresa = "Primorska bb",            Latituda = 42.9230, Longituda = 17.6120, RadnoVrijeme = "08:00 - 20:00" },
            new() { Grad = mostar,    Naziv = "SunnyRides Mostar",    Adresa = "Kralja Tomislava 12",     Latituda = 43.3438, Longituda = 17.8078, RadnoVrijeme = "08:00 - 18:00" },
            new() { Grad = split,     Naziv = "SunnyRides Split",     Adresa = "Obala kneza Domagoja 5",  Latituda = 43.5081, Longituda = 16.4402, RadnoVrijeme = "07:00 - 21:00" },
            new() { Grad = dubrovnik, Naziv = "SunnyRides Dubrovnik", Adresa = "Vukovarska 20",           Latituda = 42.6507, Longituda = 18.0944, RadnoVrijeme = "07:00 - 21:00" }
        };
        _context.Poslovnice.AddRange(_poslovnice);

        // --- tipovi vozila, marke, gorivo ---
        var skuter = new TipVozila { Naziv = "Skuter" };
        var motocikl = new TipVozila { Naziv = "Motocikl" };
        var quad = new TipVozila { Naziv = "Quad" };
        _tipoviVozila = new List<TipVozila> { skuter, motocikl, quad };
        _context.TipoviVozila.AddRange(_tipoviVozila);

        var vespa = new Marka { Naziv = "Vespa" };
        var honda = new Marka { Naziv = "Honda" };
        var yamaha = new Marka { Naziv = "Yamaha" };
        var kawasaki = new Marka { Naziv = "Kawasaki" };
        var niu = new Marka { Naziv = "NIU" };
        var cfmoto = new Marka { Naziv = "CFMoto" };
        _marke = new List<Marka> { vespa, honda, yamaha, kawasaki, niu, cfmoto };
        _context.Marke.AddRange(_marke);

        var benzin = new TipGoriva { Naziv = "Benzin" };
        var dizel = new TipGoriva { Naziv = "Dizel" };
        var elektricni = new TipGoriva { Naziv = "Elektricni", JeElektricni = true };
        _context.TipoviGoriva.AddRange(benzin, dizel, elektricni);

        // --- kategorije dozvola ---
        var a1 = new KategorijaDozvole { Oznaka = "A1", Opis = "Skuteri i motocikli do 125 cm3 i do 11 kW" };
        var a = new KategorijaDozvole { Oznaka = "A", Opis = "Svi motocikli i skuteri, bez ogranicenja" };
        var b = new KategorijaDozvole { Oznaka = "B", Opis = "Quadovi i laka cetverocikla vozila" };
        _kategorije = new List<KategorijaDozvole> { a1, a, b };
        _context.KategorijeDozvola.AddRange(_kategorije);

        // --- pravila kategorija ---
        // Ovo je podatak, ne if grana u kodu. Hijerarhija "A pokriva i A1" je posljedica
        // toga sto A nema ogranicenja tamo gdje A1 ima, a ne posebnog pravila u kodu.
        _context.PravilaKategorija.AddRange(
            new PravilaKategorije { KategorijaDozvole = a1, TipVozila = skuter,   MaxKubikaza = 125,  MaxSnagaKw = 11m, MinGodine = 16 },
            new PravilaKategorije { KategorijaDozvole = a1, TipVozila = motocikl, MaxKubikaza = 125,  MaxSnagaKw = 11m, MinGodine = 16 },
            new PravilaKategorije { KategorijaDozvole = a,  TipVozila = skuter,   MaxKubikaza = null, MaxSnagaKw = null, MinGodine = 24 },
            new PravilaKategorije { KategorijaDozvole = a,  TipVozila = motocikl, MaxKubikaza = null, MaxSnagaKw = null, MinGodine = 24 },
            new PravilaKategorije { KategorijaDozvole = b,  TipVozila = quad,     MaxKubikaza = null, MaxSnagaKw = null, MinGodine = 18 }
        );

        // --- modeli vozila ---
        void Model(Marka marka, TipVozila tip, TipGoriva gorivo, KategorijaDozvole kat,
                   string naziv, int kubikaza, decimal snaga, string slika)
        {
            var m = new ModelVozila
            {
                Marka = marka, TipVozila = tip, TipGoriva = gorivo, KategorijaDozvole = kat,
                Naziv = naziv, Kubikaza = kubikaza, SnagaKw = snaga
            };
            _modeli.Add(m);
            _slikaModela[naziv] = slika;
        }

        Model(vespa,    skuter,   benzin,     a1, "Primavera 125", 125, 7.9m,  "vespa-primavera-125");
        Model(honda,    skuter,   benzin,     a1, "PCX 125",       125, 9.0m,  "honda-pcx-125");
        Model(yamaha,   skuter,   benzin,     a1, "NMAX 125",      125, 9.0m,  "yamaha-nmax-125");
        Model(niu,      skuter,   elektricni, a1, "NQi GT",          0, 3.5m,  "niu-nqi-gt");
        Model(yamaha,   skuter,   benzin,     a,  "TMAX 560",      562, 35.0m, "yamaha-tmax-560");
        Model(honda,    motocikl, benzin,     a1, "CB125R",        125, 11.0m, "honda-cb125r");
        Model(honda,    motocikl, benzin,     a,  "CB500F",        471, 35.0m, "honda-cb500f");
        Model(kawasaki, motocikl, benzin,     a,  "Z650",          649, 50.0m, "kawasaki-z650");
        Model(cfmoto,   quad,     benzin,     b,  "CForce 520",    495, 27.0m, "cfmoto-cforce-520");
        Model(yamaha,   quad,     benzin,     b,  "Kodiak 700",    686, 35.0m, "yamaha-kodiak-700");

        _context.ModeliVozila.AddRange(_modeli);

        // --- dodatna oprema ---
        _vrsteOpreme = new List<VrstaOpreme>
        {
            new() { Naziv = "Kaciga",           CijenaPoDanu = 5m,  FiksnaCijena = null },
            new() { Naziv = "GPS uredjaj",      CijenaPoDanu = 7m,  FiksnaCijena = null },
            new() { Naziv = "Top-case kofer",   CijenaPoDanu = 4m,  FiksnaCijena = null },
            new() { Naziv = "Rukavice",         CijenaPoDanu = 3m,  FiksnaCijena = null },
            new() { Naziv = "Zastitni prsluk",  CijenaPoDanu = null, FiksnaCijena = 10m }
        };
        _context.VrsteOpreme.AddRange(_vrsteOpreme);

        // --- zalihe opreme po poslovnici ---
        foreach (var vrsta in _vrsteOpreme)
        {
            foreach (var poslovnica in _poslovnice)
            {
                _context.StanjaOpreme.Add(new StanjeOpreme
                {
                    VrstaOpreme = vrsta,
                    Poslovnica = poslovnica,
                    Kolicina = Broj(6, 31)
                });
            }
        }

        // --- paketi osiguranja ---
        _paketiOsiguranja = new List<PaketOsiguranja>
        {
            new() { Naziv = "Osnovno",     CijenaPoDanu = 8m,  IznosUcesca = 300m },
            new() { Naziv = "Prosireno",   CijenaPoDanu = 15m, IznosUcesca = 150m },
            new() { Naziv = "Puno kasko",  CijenaPoDanu = 25m, IznosUcesca = 0m }
        };
        _context.PaketiOsiguranja.AddRange(_paketiOsiguranja);

        // --- razlozi otkazivanja ---
        // Klijent i agencija imaju svoje razloge. Vremenski uslovi i "Ostalo" nude se
        // i jednima i drugima, a "Ostalo" trazi da se upise objasnjenje.
        var vrijeme = new RazlogOtkazivanja { Naziv = "Nepovoljni vremenski uslovi", ZaKlijenta = true, ZaAgenciju = true };
        var ostalo = new RazlogOtkazivanja { Naziv = "Ostalo", ZaKlijenta = true, ZaAgenciju = true, TraziNapomenu = true };

        _razloziKlijenta = new List<RazlogOtkazivanja>
        {
            new() { Naziv = "Promjena planova putovanja", ZaKlijenta = true },
            new() { Naziv = "Pronadjena povoljnija ponuda", ZaKlijenta = true },
            new() { Naziv = "Pogresno odabran termin ili vozilo", ZaKlijenta = true },
            new() { Naziv = "Zdravstveni razlozi", ZaKlijenta = true },
            vrijeme
        };

        _razloziAgencije = new List<RazlogOtkazivanja>
        {
            new() { Naziv = "Vozilo je u kvaru ili na servisu", ZaAgenciju = true },
            new() { Naziv = "Klijent nema vazecu vozacku dozvolu", ZaAgenciju = true },
            new() { Naziv = "Klijent nije dostupan za potvrdu termina", ZaAgenciju = true },
            vrijeme
        };

        // Sistemski razlog: njime worker otkazuje rezervaciju koja nije placena u roku.
        // Nije aktivan i ne nudi se nijednoj strani, pa ga niko ne moze izabrati rucno.
        var isteklo = new RazlogOtkazivanja
        {
            Naziv = "Isteklo vrijeme za placanje",
            ZaKlijenta = false,
            ZaAgenciju = false,
            Aktivan = false
        };

        _context.RazloziOtkazivanja.AddRange(
            _razloziKlijenta.Concat(_razloziAgencije).Append(ostalo).Append(isteklo).Distinct());

        await _context.SaveChangesAsync(ct);
    }
}
