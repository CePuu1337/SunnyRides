using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Database.Seed;

public partial class DatabaseSeeder
{
    private List<Vozilo> _vozila = new();

    /// <summary>Koliko primjeraka svakog modela ide u flotu. Ukupno 35 vozila.</summary>
    private static readonly Dictionary<string, int> BrojPrimjeraka = new()
    {
        ["Primavera 125"] = 5,
        ["PCX 125"] = 5,
        ["NMAX 125"] = 4,
        ["NQi GT"] = 3,
        ["TMAX 560"] = 3,
        ["CB125R"] = 4,
        ["CB500F"] = 3,
        ["Z650"] = 3,
        ["CForce 520"] = 3,
        ["Kodiak 700"] = 2
    };

    /// <summary>Satna tarifa, dnevna tarifa i depozit po modelu.</summary>
    private static readonly Dictionary<string, (decimal satna, decimal dnevna, decimal depozit)> Tarife = new()
    {
        ["Primavera 125"] = (8m, 35m, 150m),
        ["PCX 125"] = (8m, 33m, 150m),
        ["NMAX 125"] = (8m, 34m, 150m),
        ["NQi GT"] = (7m, 30m, 150m),
        ["TMAX 560"] = (15m, 75m, 400m),
        ["CB125R"] = (9m, 38m, 200m),
        ["CB500F"] = (14m, 70m, 350m),
        ["Z650"] = (16m, 85m, 450m),
        ["CForce 520"] = (18m, 88m, 500m),
        ["Kodiak 700"] = (19m, 95m, 550m)
    };

    private async Task SeedFlotaAsync(CancellationToken ct)
    {
        var koristeneOznake = new HashSet<string>();
        var slova = "ABEJKMOT";

        string Registracija()
        {
            string oznaka;
            do
            {
                oznaka = $"{slova[Broj(0, slova.Length)]}{Broj(10, 100)}-"
                       + $"{slova[Broj(0, slova.Length)]}-{Broj(100, 1000)}";
            }
            while (!koristeneOznake.Add(oznaka));
            return oznaka;
        }

        // --- vozila ---
        var indeksPoslovnice = 0;
        foreach (var model in _modeli)
        {
            var (satna, dnevna, depozit) = Tarife[model.Naziv];
            var slika = _slikaModela[model.Naziv];

            for (var i = 0; i < BrojPrimjeraka[model.Naziv]; i++)
            {
                // Vozila se redom raspodjeljuju po poslovnicama, da nijedna ne ostane prazna.
                var poslovnica = _poslovnice[indeksPoslovnice % _poslovnice.Count];
                indeksPoslovnice++;

                var vozilo = new Vozilo
                {
                    ModelVozila = model,
                    Poslovnica = poslovnica,
                    RegistarskaOznaka = Registracija(),
                    GodinaProizvodnje = Broj(2019, 2027),
                    Kilometraza = Broj(1_200, 42_000),
                    Aktivno = true,
                    SatnaTarifa = satna,
                    DnevnaTarifa = dnevna,
                    IznosDepozita = depozit,
                    DatumKreiranja = _danas.AddDays(-Broj(200, 900))
                };

                // Slika ide kao putanja do fajla; u bazu nikad ne ide sadrzaj slike.
                // Lista vozila cita samo PutanjaThumbnail.
                vozilo.Slike.Add(new SlikaVozila
                {
                    Putanja = $"/uploads/seeds/{slika}.jpg",
                    PutanjaThumbnail = $"/uploads/seeds/thumbs/{slika}.jpg",
                    Redoslijed = 0,
                    JeGlavna = true
                });

                _vozila.Add(vozilo);
                _context.Vozila.Add(vozilo);
            }
        }

        // Dva vozila su deaktivirana - da se vidi razlika izmedju deaktivacije i brisanja.
        _vozila[7].Aktivno = false;
        _vozila[24].Aktivno = false;

        // --- cjenovnik ---
        // Dvije sezone godisnje: ljetna sa uvecanjem, ostatak godine sa umanjenjem.
        // Pragovi popusta su isti kao u PricingService: 3+ dana 5 %, 7+ dana 10 %.
        var godina = _danas.Year;
        foreach (var model in _modeli)
        {
            for (var pomak = -1; pomak <= 1; pomak++)
            {
                var g = godina + pomak;

                _context.Cjenovnici.Add(new Cjenovnik
                {
                    ModelVozila = model,
                    Naziv = $"Glavna sezona {g}",
                    DatumOd = new DateTime(g, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    DatumDo = new DateTime(g, 9, 30, 23, 59, 59, DateTimeKind.Utc),
                    Mnozilac = 1.30m,
                    PopustPrag1 = 3, PopustProcenat1 = 5m,
                    PopustPrag2 = 7, PopustProcenat2 = 10m
                });

                _context.Cjenovnici.Add(new Cjenovnik
                {
                    ModelVozila = model,
                    Naziv = $"Vansezona {g}/{g + 1}",
                    DatumOd = new DateTime(g, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    DatumDo = new DateTime(g + 1, 5, 31, 23, 59, 59, DateTimeKind.Utc),
                    Mnozilac = 0.85m,
                    PopustPrag1 = 3, PopustProcenat1 = 5m,
                    PopustPrag2 = 7, PopustProcenat2 = 10m
                });
            }
        }

        // --- blokade vozila (servis) ---
        var razloziBlokade = new[]
        {
            "Redovan servis na 10.000 km",
            "Zamjena guma i kocionih plocica",
            "Popravka nakon manjeg ostecenja",
            "Tehnicki pregled",
            "Zamjena akumulatora"
        };

        for (var i = 0; i < 6; i++)
        {
            var vozilo = _vozila[Broj(0, _vozila.Count)];
            var pocetak = _danas.AddDays(Broj(-120, 45)).AddHours(8);

            _context.BlokadeVozila.Add(new BlokadaVozila
            {
                Vozilo = vozilo,
                DatumOd = pocetak,
                DatumDo = pocetak.AddDays(Broj(1, 5)).AddHours(10),
                Razlog = Izaberi(razloziBlokade),
                KreiraoKorisnik = _uposlenik,
                DatumKreiranja = pocetak.AddDays(-Broj(1, 10))
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
