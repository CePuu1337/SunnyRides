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

## Pokretanje za pregled rada

Ovako se rad pokreće iz predanog stanja, bez ikakve izmjene koda, portova ili
konekcijskih stringova. Potrebni su samo **Docker Desktop** i **Android emulator**
(AVD), za mobilnu aplikaciju.

**1. Konfiguracija.** U korijenu repozitorija stoji `.env-tajne.zip`. Raspakuj ga na
isto mjesto, pa pored `docker-compose.yml` nastane `.env`. Šifra je predana uz rad na
DLWMS-u. Arhiva je šifrovana AES-256 algoritmom, koji Windows Explorer ne otvara, pa
treba 7-Zip ili WinRAR:

```powershell
& "C:\Program Files\7-Zip\7z.exe" x .env-tajne.zip
```

Može i desnim klikom na fajl → 7-Zip → Extract Here.

**2. Backend.** Iz korijena repozitorija:

```bash
docker compose up --build
```

Prvo pokretanje povlači SQL Server i RabbitMQ image-e i traje nekoliko minuta. API sam
napravi bazu `220182`, primijeni migracije i napuni je demo podacima. Spreman je kad u
logu piše „Baza je spremna." i kad se otvara `http://localhost:5000/swagger`.

Servisi imaju `restart: unless-stopped`, pa se poslije restarta Dockera ili računara
sami ponovo podignu.

Buildovi aplikacija su u ZIP arhivi na GitHub Release-u, ne u repozitoriju. Unutar
arhive su na istim putanjama na kojima ih Flutter pravi:

```
sunnyrides_desktop/build/windows/x64/runner/Release/sunnyrides_desktop.exe
sunnyrides_mobile/build/app/outputs/flutter-apk/app-release.apk
```

**3. Desktop aplikacija.** Pokreni `sunnyrides_desktop.exe` iz foldera `Release`. Fajl
mora ostati u tom folderu, pored `data\` i `.dll` fajlova. Spaja se na
`http://localhost:5000`.

**4. Mobilna aplikacija.** Pokreni emulator, obriši eventualnu staru verziju
aplikacije, pa prevuci `app-release.apk` na prozor emulatora.
Aplikacija se spaja na `http://10.0.2.2:5000`, što je za Android emulator `localhost`
host računara.

**Mailovi.** Demo korisnici imaju adrese na rezervisanim domenama (`@example.com`,
`@sunnyrides.example`), na koje pošta ne može stići, pa njihove mailove worker šalje na
adresu autora. Nalog otvoren kroz mobilnu aplikaciju sa stvarnim emailom dobija
potvrde, obavještenja i kod za reset lozinke na taj email. Obavještenja u aplikaciji
stižu svima, u realnom vremenu.

---

## Razvoj

Za rad na kodu još trebaju:

- .NET SDK 9.0 ili noviji
- Flutter (stabilna grana) sa uključenim Windows desktop i Android toolchainom
- `dotnet-ef` alat: `dotnet tool install --global dotnet-ef`

**Konfiguracija.** `.env.example` je šablon sa svim ključevima i objašnjenjima. Za
vlastito okruženje kopiraj ga u `.env` i popuni:

```bash
cp .env.example .env
```

`JWT_KEY` mora biti nasumičan niz od najmanje 32 znaka. Ne izmišljaj ga rukom:

```powershell
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Kad su servisi podignuti:

- API je na `http://localhost:5000`, Swagger na `http://localhost:5000/swagger`
- RabbitMQ konzola je na `http://localhost:15672`, prijava `guest` / `guest`
- Baza je na `localhost,1433`, korisnik `sa`

**Rad na backendu.** Praktičnije je dići samo infrastrukturu pa API pokretati iz
Visual Studija:

```bash
docker compose up -d sunnyrides-db sunnyrides-rabbitmq
dotnet ef database update --project SunnyRides.Services --startup-project SunnyRides.API
dotnet run --project SunnyRides.API
```

> **Zašto to radi bez izmjene konfiguracije.** Unutar Docker mreže baza je
> `sunnyrides-db`, a sa tvoje mašine `localhost`. `.env` drži `localhost` varijantu,
> a `docker-compose.yml` je za kontejnere prepisuje na ime servisa. Oba načina rade
> bez diranja fajlova.

**Flutter aplikacije.** Adresa API-ja se zadaje komandom, a bez nje se uzima
podrazumijevana (`localhost` za desktop, `10.0.2.2` za emulator):

```bash
# desktop
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000

# mobilna, na Android emulatoru
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

**Release buildovi**, isti kao oni u GitHub Release-u:

```bash
cd sunnyrides_mobile
flutter clean
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000

cd ../sunnyrides_desktop
flutter clean
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000
```

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
├── .env-tajne.zip              # .env pod šifrom, za pregled rada
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
