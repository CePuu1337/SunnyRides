# SunnyRides

Sistem za rezervaciju i najam skutera, motocikala i quadova. Jedna agencija sa više
poslovnica upravlja svojom flotom; klijenti kroz mobilnu aplikaciju pretražuju
slobodna vozila, rezervišu i plaćaju unaprijed, a administrator i uposlenici kroz
desktop aplikaciju vode flotu, rezervacije i primopredaju vozila.

Seminarski rad iz predmeta **Razvoj softvera II** · Ammar Puce, IB220182

---

## Šta čini sistem

| Dio | Tehnologija | Ko ga koristi |
|---|---|---|
| REST API | ASP.NET Core, .NET 9 | — |
| Worker servis | .NET 9, zaseban kontejner | — |
| Desktop aplikacija | Flutter Windows | administrator, uposlenik |
| Mobilna aplikacija | Flutter Android | klijent |

Uz to: SQL Server i RabbitMQ u Dockeru, Stripe sandbox za plaćanje i SignalR za
notifikacije u realnom vremenu.

Dvije stvari su srž ovog sistema. Prva je da isto vozilo ne smije biti izdato dvaput
u periodima koji se preklapaju. Druga je da se klijentu ne smije ponuditi vozilo koje
prema kategoriji svoje vozačke dozvole ne smije voziti. Sve ostalo je oko toga.

---

## Šta treba imati instalirano

- .NET SDK 9.0 ili noviji
- Docker Desktop
- Flutter 3.x sa uključenim Windows desktop i Android toolchainom
- `dotnet-ef` alat: `dotnet tool install --global dotnet-ef`

---

## Pokretanje

**1. Konfiguracija.** Kopiraj `.env.example` u `.env` i popuni vrijednosti:

```bash
cp .env.example .env
```

Za lokalni rad dovoljno je postaviti `DB_SA_PASSWORD`, `CONNECTION_STRING`,
`RABBITMQ_PASSWORD` i `JWT_KEY`. Stripe i SMTP vrijednosti trebaju tek za plaćanje
i slanje emaila.

`JWT_KEY` mora biti nasumičan niz od najmanje 32 znaka. Ne izmišljaj ga rukom:

```powershell
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

**2. Podigni sve:**

```bash
docker compose up --build
```

Prvi put povlači SQL Server i RabbitMQ image-e, pa potraje. Kad završi:

- API je na `http://localhost:5000`, Swagger na `http://localhost:5000/swagger`
- RabbitMQ konzola je na `http://localhost:15672`, prijava `guest` / `guest`
- Baza je na `localhost,1433`, korisnik `sa`

**3. Ako radiš na backendu**, praktičnije je dići samo infrastrukturu pa API
pokretati iz Visual Studija:

```bash
docker compose up -d sunnyrides-db sunnyrides-rabbitmq
dotnet ef database update --project SunnyRides.Services --startup-project SunnyRides.API
dotnet run --project SunnyRides.API
```

> **Zašto to radi bez izmjene konfiguracije.** Unutar Docker mreže baza je
> `sunnyrides-db`, a sa tvoje mašine `localhost`. `.env` drži `localhost` varijantu,
> a `docker-compose.yml` je za kontejnere prepisuje na ime servisa. Oba načina rade
> bez diranja fajlova.

**4. Flutter aplikacije:**

```bash
# desktop
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000

# mobilna, na Android emulatoru
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Adresa `10.0.2.2` nije greška — tako Android emulator vidi `localhost` host mašine.

---

## Kredencijali

Sve lozinke su `test`.

| Kontekst | Korisničko ime | Lozinka |
|---|---|---|
| Desktop verzija | `desktop` | `test` |
| Mobilna verzija | `mobile` | `test` |
| Administrator | `administrator` | `test` |
| Uposlenik | `uposlenik` | `test` |

Nalozi se kreiraju seedom pri prvom pokretanju API-ja nad praznom bazom.

Plaćanje ide kroz Stripe sandbox. Testna kartica je `4242 4242 4242 4242`, bilo koji
budući datum isteka i bilo koji CVC (npr. `123`). Kartica `4000 0000 0000 9995` se
odbija, pa se na njoj vidi i neuspjelo plaćanje.

---

## Struktura repozitorija

```
SunnyRides/
├── docker-compose.yml
├── .env                        # tajne, nije u gitu
├── .env.example                # šablon, jeste u gitu
├── DOKUMENTACIJA.md            # kako sistem radi iznutra
├── recommender-dokumentacija.md
│
├── SunnyRides.Model/           # DTO, request i search objekti, enumi
├── SunnyRides.Services/        # entiteti, DbContext, migracije, poslovna logika
├── SunnyRides.API/             # kontroleri, filteri, SignalR hub
├── SunnyRides.Subscriber/      # worker: RabbitMQ consumeri, email, periodični poslovi
├── SunnyRides.Tests/           # unit testovi (cijena, povrat, dozvole, preporuke...)
│
├── uploads/seeds/              # slike vozila za seed, jesu u gitu
├── privatno/                   # dozvole i fotografije štete, nije u gitu
│
├── sunnyrides_core/            # zajednički Flutter paket: API klijent, modeli, tema
├── sunnyrides_desktop/         # Flutter Windows
└── sunnyrides_mobile/          # Flutter Android
```

Zavisnosti idu samo u jednom smjeru: **API → Services → Model**. `Model` ne
referencira ništa.

---

## Korisne komande

```bash
# nova migracija
dotnet ef migrations add NazivMigracije --project SunnyRides.Services --startup-project SunnyRides.API

# primijeni migracije
dotnet ef database update --project SunnyRides.Services --startup-project SunnyRides.API

# provjeri da baza stvarno postoji
docker exec sunnyrides-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "<lozinka>" \
  -Q "USE [220182]; SELECT COUNT(*) FROM sys.tables;"

# pogledaj poruke u RabbitMQ-u
# http://localhost:15672

# čisto okruženje, kao da je prvi put
docker compose down -v
docker compose up --build
```

---

## Više detalja

`DOKUMENTACIJA.md` opisuje kako sistem radi iznutra: kako je podijeljen kod, šta
server računa a šta prima od klijenta, kojim redoslijedom se validiraju zahtjevi,
šta štiti od dva istovremena zahtjeva i kako teče životni ciklus rezervacije.
