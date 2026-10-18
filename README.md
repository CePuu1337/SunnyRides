# SunnyRides

Sistem za rezervaciju i najam skutera, motocikala i quadova. Agencija sa više
poslovnica kroz desktop aplikaciju vodi flotu, rezervacije i primopredaju vozila, a
klijenti kroz mobilnu aplikaciju traže slobodna vozila, rezervišu ih i plaćaju.

Seminarski rad iz predmeta Razvoj softvera II, Ammar Puce, IB220182.

## Dijelovi sistema

| Dio | Tehnologija | Korisnici |
|---|---|---|
| REST API | ASP.NET Core, .NET 9 | |
| Worker (mailovi, obavještenja, periodični poslovi) | .NET 9, zaseban kontejner | |
| Desktop aplikacija | Flutter, Windows | administrator, uposlenik |
| Mobilna aplikacija | Flutter, Android | klijent |

Baza je SQL Server, a API i worker komuniciraju preko RabbitMQ-a. Sve se pokreće kroz
Docker Compose. Plaćanje ide preko Stripe-a u testnom režimu, a obavještenja stižu u
realnom vremenu preko SignalR-a.

## Pokretanje

Potrebni su Docker Desktop i Android emulator.

### 1. Konfiguracija

U korijenu repozitorija je `.env-tajne.zip`. Raspakujte ga na isto mjesto, tako da
`.env` bude pored `docker-compose.yml`. Šifra je predana na DLWMS-u. Arhiva je
zaštićena AES-256 enkripcijom, koju Windows Explorer ne otvara, pa treba 7-Zip ili
WinRAR:

```powershell
& "C:\Program Files\7-Zip\7z.exe" x .env-tajne.zip
```

### 2. Backend

```bash
docker compose up --build
```

Komanda se pokreće iz korijena repozitorija, gdje su `docker-compose.yml` i `.env`.
Prvo pokretanje traje nekoliko minuta, dok se ne preuzmu image-i i NuGet paketi. API
sam napravi bazu `220182`, primijeni migracije i napuni je demo podacima. Spreman je
kad se u logu pojavi „Baza je spremna." i kad se otvara
`http://localhost:5000/swagger`.

Kontejneri se poslije restarta računara ili Dockera sami ponovo podignu.

### 3. Aplikacije

Buildovi su u ZIP arhivi na GitHub Release-u, na istim putanjama na kojima ih Flutter
pravi:

```
sunnyrides_desktop/build/windows/x64/runner/Release/sunnyrides_desktop.exe
sunnyrides_mobile/build/app/outputs/flutter-apk/app-release.apk
```

- Desktop: pokrenite `sunnyrides_desktop.exe` iz foldera `Release`. Fajl mora ostati u
  tom folderu, pored `data` i `.dll` fajlova. Aplikacija se spaja na
  `http://localhost:5000`.
- Mobilna: pokrenite emulator, obrišite staru verziju aplikacije ako postoji i
  prevucite `app-release.apk` na prozor emulatora. Aplikacija se spaja na
  `http://10.0.2.2:5000`, što je iz emulatora adresa računara.

### Ako nešto ne radi

- Docker javlja da je port zauzet: portovi 1433, 5000, 5672 i 15672 moraju biti
  slobodni. Najčešće je to lokalno instaliran SQL Server na portu 1433, koji treba
  zaustaviti.
- Desktop aplikacija javlja da nedostaje `VCRUNTIME140_1.dll` ili `MSVCP140.dll`:
  na računaru nije instaliran Microsoft Visual C++ Redistributable (x64), koji
  Flutter aplikacije na Windowsu koriste.
- Mobilna aplikacija ne može doći do servera: API mora raditi na istom računaru na
  kojem je emulator. Plaćanje uz to traži da emulator ima pristup internetu, jer
  Stripe radi preko interneta.
- Za pokretanje od nule, sa praznom bazom i ponovo napunjenim demo podacima:
  `docker compose down -v`, pa `docker compose up --build`.

## Pristupni podaci

Lozinka je svuda `test`.

| Kontekst | Korisničko ime | Lozinka |
|---|---|---|
| Desktop verzija | `desktop` | `test` |
| Mobilna verzija | `mobile` | `test` |
| Administrator | `administrator` | `test` |
| Uposlenik | `uposlenik` | `test` |

- `desktop` i `administrator` su administratori i vide sve poslovnice.
- `uposlenik` radi u poslovnici u Mostaru i vidi samo nju. Izvještaje ima samo
  administrator.
- `mobile` je klijent iz Sarajeva. Sarajevo nema poslovnicu, pa mu je najbliža Mostar.
- Klijent koji se registruje kroz aplikaciju bira grad odmah nakon registracije, a
  vozačku dozvolu prijavljuje u profilu. Dok mu je uposlenik ne odobri, može
  pretraživati vozila, ali ne i rezervisati.

### Testno plaćanje

| Kartica | Ishod |
|---|---|
| `4242 4242 4242 4242` | uspješno plaćanje |
| `4000 0000 0000 9995` | odbijeno |

Datum isteka može biti bilo koji budući, a CVC bilo koji, npr. `123`. Nijedna kartica
se stvarno ne tereti.

### Mailovi

Demo korisnici imaju adrese na domenama `@example.com` i `@sunnyrides.example`, na koje
pošta ne može stići, pa se njihovi mailovi preusmjeravaju na adresu iz
`SMTP_PREUSMJERI_NA` u `.env`. Klijent registrovan sa stvarnim emailom dobija mailove
na svoju adresu, uključujući kod za reset lozinke.

## Reset lozinke bez prijave

Pravila predmeta dozvoljavaju `[AllowAnonymous]` samo na prijavi i registraciji. Reset
zaboravljene lozinke je jedini izuzetak: ko je zaboravio lozinku nema token, pa oba
koraka (`/api/auth/zaboravljena-lozinka` i `/api/auth/reset-lozinke`) moraju raditi bez
prijave. Kod stiže samo na email, u bazi se čuva kao hash i važi ograničeno vrijeme, a
odgovor na prvi korak je isti bez obzira na to postoji li nalog sa tom adresom. Test
`PravilaAutorizacijeTests` pada ako se bilo gdje drugo pojavi endpoint dostupan bez
prijave.

## Razvoj

Za rad na kodu trebaju još .NET SDK 9, Flutter (stabilna grana, sa podrškom za Windows
i Android) i alat za migracije:

```bash
dotnet tool install --global dotnet-ef
```

`.env.example` sadrži sve ključeve sa objašnjenjima. Za vlastito okruženje kopirajte ga
u `.env` i popunite vrijednosti.

Backend je najlakše razvijati tako da u Dockeru rade samo baza i RabbitMQ, a API se
pokreće iz Visual Studija ili terminala:

```bash
docker compose up -d sunnyrides-db sunnyrides-rabbitmq
dotnet run --project SunnyRides.API
```

`.env` koristi `localhost` za bazu i RabbitMQ, a `docker-compose.yml` te adrese za
kontejnere zamjenjuje imenima servisa, pa oba načina rade bez mijenjanja
konfiguracije.

Flutter aplikacije adresu API-ja čitaju iz `API_BASE_URL`. Bez nje desktop koristi
`http://localhost:5000`, a mobilna `http://10.0.2.2:5000`:

```bash
cd sunnyrides_desktop
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000

cd sunnyrides_mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Release buildovi:

```bash
cd sunnyrides_mobile
flutter clean
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000

cd sunnyrides_desktop
flutter clean
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000
```

Ostale komande:

```bash
# testovi
dotnet test

# nova migracija
dotnet ef migrations add NazivMigracije --project SunnyRides.Services --startup-project SunnyRides.API

# pokretanje od nule, sa praznom bazom
docker compose down -v
docker compose up --build
```

Swagger je na `http://localhost:5000/swagger`, a RabbitMQ konzola na
`http://localhost:15672` (`guest` / `guest`).

## Struktura

```
SunnyRides/
├── docker-compose.yml
├── .env.example
├── .env-tajne.zip               .env pod šifrom
├── recommender-dokumentacija.md opis sistema preporuke
│
├── SunnyRides.Model/            DTO, request i search objekti, enumi
├── SunnyRides.Services/         entiteti, DbContext, migracije, poslovna logika
├── SunnyRides.API/              kontroleri, SignalR hub
├── SunnyRides.Subscriber/       worker: RabbitMQ, mailovi, periodični poslovi
├── SunnyRides.Tests/            unit testovi
│
├── uploads/seeds/               slike za demo podatke
│
├── sunnyrides_core/             zajednički Flutter kod: API klijent, modeli, tema
├── sunnyrides_desktop/          desktop aplikacija
└── sunnyrides_mobile/           mobilna aplikacija
```
