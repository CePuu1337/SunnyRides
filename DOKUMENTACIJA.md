# Kako SunnyRides radi

**Seminarski rad iz predmeta Razvoj softvera II · Ammar Puce, IB220182**

Ovaj dokument opisuje kako je sistem sastavljen i zašto je sastavljen baš tako.
Nije to spisak želja — svaka tvrdnja ovdje treba odgovarati kodu koji stvarno stoji
u repozitoriju. Gdje nešto još nije napravljeno, to i piše.

Razlog za takvu strogost je prost: dokumentacija koja opisuje nepostojeći kod je
gora nego da je nema. Uputstvo za seminarski rad to na dva mjesta izričito kažnjava,
a i praktično — jedina stvar zbog koje se ovakav fajl uopšte piše jeste da mu se
može vjerovati kad zatreba.

> **Kako se održava.** Kad se završi faza, popuni se odgovarajuća sekcija i pomjeri
> oznaka statusa, u istom commitu kao i kod. Ako se dokument mijenja odvojeno od
> koda, za mjesec dana više nije tačan.

Oznake kroz dokument: ✅ urađeno · 🟡 djelimično · ⬜ još nije.

---

## Gdje smo trenutno

| Faza | Šta | |
|---|---|---|
| 0–3 | Okruženje, repozitorij, skeleton solutiona, Docker Compose | ✅ |
| 4 | Model baze i prva migracija | ✅ |
| 4 | Seed podaci | ✅ |
| 5 | Bazni servisi, paginacija, `ExceptionFilter`, Mapster, Swagger | ✅ |
| 6 | Prijava, JWT, uloge, opoziv tokena | ✅ |
| 7 | CRUD referentnih podataka | ✅ |
| 8 | Vozila, slike, blokade, cjenovnik | 🟡 |
| 9 | Obračun cijene i provjera dostupnosti | 🟡 |
| 10 | Vozačke dozvole i filtriranje po kategoriji | ⬜ |
| 11 | Rezervacije i state machine | ⬜ |
| 12 | Plaćanje, webhook, povrat novca | ⬜ |
| 13 | Primopredaja i obračun depozita | ⬜ |
| 14 | RabbitMQ i worker servis | ⬜ |
| 15 | Notifikacije i SignalR | ⬜ |
| 16 | Sistem preporuke | ⬜ |
| 17–18 | Desktop i mobilna aplikacija | ⬜ |
| 19 | PDF izvještaji | ⬜ |

---

## Kako je projekat podijeljen

Backend čine četiri projekta i zavisnosti idu samo u jednom smjeru:

```
SunnyRides.API  ──▶  SunnyRides.Services  ──▶  SunnyRides.Model
                                    ▲
SunnyRides.Subscriber ──────────────┘
```

**`SunnyRides.Model`** je najniži sloj i ne zna ni za šta. U njemu su DTO klase koje
se vraćaju klijentu, request i search objekti, enumi i konstante rola. Nema EF-a,
nema `DbContext`-a, nema logike. To se može i provjeriti — `dotnet list
SunnyRides.Model reference` vraća praznu listu, i tako mora ostati.

**`SunnyRides.Services`** nosi sav stvarni posao: EF entitete, `DbContext`,
konfiguracije, migracije i poslovnu logiku. Ovdje žive pravila — koliko košta najam,
je li vozilo slobodno, smije li klijent voziti to vozilo.

**`SunnyRides.API`** je namjerno tanak. Kontroleri primaju zahtjev, pozovu servis i
vrate DTO. Ne sadrže poslovnu logiku i ne diraju `DbContext` direktno. Uz njih idu
`ExceptionFilter`, SignalR hub, middleware za provjeru opozvanih tokena i sva
konfiguracija DI kontejnera.

**`SunnyRides.Subscriber`** je worker — zaseban projekat, zaseban `Dockerfile`,
zaseban kontejner. Sluša poruke sa RabbitMQ-a i vrti periodične poslove, a ne izlaže
nijedan HTTP endpoint.

To što je worker odvojen nije stvar ukusa. Uputstvo (sekcija 3.2) kaže da
`BackgroundService` unutar API projekta ne zadovoljava zahtjev za mikroservisom,
jer radi u istom procesu kao i API. Mora biti svoj kontejner.

Tok podataka kroz slojeve izgleda ovako:

```
Kontroler  →  Servis  →  DbContext  →  baza
    ↑           ↓
   DTO   ←   Mapiranje  ←  Entitet
```

---

## Šta koristimo i zašto

Backend je .NET 9. Plan izrade dopušta „.NET 8 ili noviji", instalirani SDK je
9.0.306, pa svi projekti ciljaju `net9.0`, a Docker image-i su `sdk:9.0`,
`aspnet:9.0` i `runtime:9.0`.

| Paket | Verzija | Gdje | Čemu |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.10 | Services | ORM i SQL Server provajder, Code First |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.10 | Services, API | migracije; u API-ju jer EF alati traže startup projekat |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.10 | Services | `dotnet ef` komande |
| `Mapster` | 10.x | Services | entitet → DTO, u servisnom sloju |
| `BCrypt.Net-Next` | 4.2.0 | Services | hashiranje lozinki |
| `RabbitMQ.Client` | 7.2.2 | Services, Subscriber | objava i konzumiranje poruka |
| `Stripe.net` | 52.x | Services | payment intent, potvrda naplate, povrat |
| `QuestPDF` | 2026.8 | Services | PDF izvještaji |
| `MailKit` | 4.17 | Subscriber | SMTP — isključivo u workeru |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.x | API | validacija JWT tokena |
| `Swashbuckle.AspNetCore` | 10.x | API | Swagger UI |
| `DotNetEnv` | 3.2.0 | API, Subscriber | učitavanje `.env` |

Dvije stvari vrijedi objasniti.

**Zašto Swashbuckle, a ne ugrađeni OpenAPI.** .NET 9 template dolazi sa
`AddOpenApi()`, ali on daje samo JSON specifikaciju bez interfejsa. Za ručno
testiranje zaštićenih endpointa treba Swagger UI sa dugmetom za unos Bearer tokena,
a to daje Swashbuckle.

**Zašto su sve tri EF verzije zakucane na 9.0.10.** Bez eksplicitne verzije NuGet
povuče najnoviji EF, koji cilja noviju verziju .NET-a i ne instalira se na `net9.0`.
Uz to, `SqlServer`, `Design` i `Tools` moraju biti ista major verzija — kad se
razlikuju, `dotnet ef` pada na neočekivane načine.

Infrastruktura ide kroz Docker Compose:

| Servis | Image | Portovi |
|---|---|---|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-CU12-ubuntu-22.04` | 1433 |
| RabbitMQ | `rabbitmq:3.13-management` | 5672, 15672 |
| API | build iz `SunnyRides.API/Dockerfile` | 5000 → 8080 |
| Worker | build iz `SunnyRides.Subscriber/Dockerfile` | — |

Svi tagovi su eksplicitno verzionisani. `:latest` se ne koristi nigdje, jer uputstvo
to navodi kao grešku.

---

## Konfiguracija i tajne

Sve tajne su u `.env` fajlu u korijenu repozitorija. Taj fajl je u `.gitignore`
od prvog commita i nikad nije bio u git historiji — što je bitno, jer jednom
commitan `.env` ostaje u historiji i nakon brisanja. Uz njega ide `.env.example`
sa istim ključevima i praznim vrijednostima, i taj ide u git.

Jedna stvar je ovdje riješena drugačije nego što plan predviđa, i vrijedi znati zašto.

**Ista baza ima dvije adrese.** Unutar Docker mreže ona je `sunnyrides-db,1433` —
to je ime servisa. Ali kad se sa razvojne mašine pokrene `dotnet ef database update`,
to ime ne postoji; odatle je baza `localhost,1433`. Ako se u `.env` upiše samo jedna
varijanta, druga prestane raditi.

Rješenje: `.env` drži `localhost` varijantu, jer se odatle najčešće radi — migracije,
lokalno pokretanje API-ja, Swagger. A `docker-compose.yml` za servise
`sunnyrides-api` i `sunnyrides-subscriber` prepisuje `CONNECTION_STRING` i
`RABBITMQ_HOST` kroz `environment:` blok. Compose daje prioritet `environment:` nad
`env_file:`, pa kontejneri dobiju imena servisa, a razvojna mašina `localhost` —
bez ijedne ručne izmjene ni u jednom smjeru.

`Program.cs` učitava `.env` samo ako fajl postoji. U kontejneru ga nema i ne treba
mu — varijable dolaze iz compose-a.

---

## Baza

Baza se zove **`220182`**, po broju indeksa bez `IB` prefiksa. Pristup je Code First,
bez stored procedura. Trenutno stanje: 36 tabela, 68 indeksa, od toga 30 jedinstvenih.

### Kako su pisane konfiguracije

Svaki entitet ima svoju `IEntityTypeConfiguration<T>` klasu u
`SunnyRides.Services/Database/Configurations/`. `OnModelCreating` sadrži jednu liniju:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(SunnyRidesDbContext).Assembly);
```

Alternativa bi bila nekoliko stotina linija u jednoj metodi, u kojoj se poslije ništa
ne nalazi.

### Brisanje

Od 49 veza u modelu, **37 je `Restrict`, a 12 `Cascade`**.

`Cascade` ide samo tamo gdje podređeni zapis nema smisla bez roditelja: slike i
blokade vozila, stavke opreme i historija statusa unutar rezervacije, povrati unutar
plaćanja, fotografije i evidencija štete unutar primopredaje, kategorije unutar
dozvole, veze korisnik–uloga, kodovi za reset lozinke, te notifikacije i historija
pretraga korisnika.

Sve ostalo je `Restrict`. Vozilo koje ima rezervacije, grad koji ima poslovnice,
korisnik koji ima historiju — ništa od toga se ne briše. Kad neko pokuša, servisni
sloj to hvata **prije** EF-a i vraća `BusinessException` sa konkretnom porukom, a ne
`DbUpdateException` koji bi klijentu stigao kao 500.

To prati uputstvo (sekcija 3.1): brisanje smije biti kaskadno kad ima smisla, a mora
biti onemogućeno kad zapis koriste drugi entiteti, uz jasnu poruku o razlogu.

### Indeksi koji nose poslovna pravila

Neki indeksi nisu tu zbog brzine nego zato što su jedina stvarna garancija da se
nešto ne desi dvaput. Vrijedi ih znati napamet:

| Indeks | Šta stvarno sprječava |
|---|---|
| `IX_Placanje_RezervacijaId` unique, filter `[Status] = 3` | dva uspješna plaćanja iste rezervacije |
| `IX_Rezervacija_KorisnikId_VoziloId_DatumOd` unique | dvostruko slanje iste forme |
| `IX_Recenzija_KorisnikId_RezervacijaId` unique | dvije recenzije za isti najam |
| `IX_Primopredaja_RezervacijaId_Tip` unique | dva izdavanja ili dva povrata iste rezervacije |
| `IX_ObradjeniWebhookEvent_ProviderEventId` unique | ponovnu obradu istog Stripe događaja |
| `IX_StanjeOpreme_VrstaOpremeId_PoslovnicaId` unique | duple zapise o zalihama |
| `IX_Korisnik_KorisnickoIme` unique | dva naloga sa istim korisničkim imenom |
| `IX_Korisnik_Email` unique | dva naloga sa istim emailom |
| `IX_Rezervacija_VoziloId_Status_DatumOd_DatumDo` | ovo je zbog brzine — nosi provjeru preklapanja |

Filtrirani indeks na plaćanju je najzanimljiviji. Rezervacija smije imati više
zapisa o plaćanju — neuspjeli pokušaji, otkazani intenti — ali najviše **jedan** sa
statusom `Succeeded` (vrijednost 3). Običan jedinstveni indeks to ne bi mogao izraziti.

### Odluke koje nisu bile u početnoj specifikaciji

Nakon poređenja specifikacije sa uputstvom, četiri stvari su nedostajale:

**`KorisnickoIme` na `Korisnik`.** Specifikacija je predviđala prijavu emailom, ali
uputstvo (sekcija 5) daje tabelu kredencijala u kojoj su korisnička imena `desktop`,
`mobile` i naziv uloge. Bez tog polja seed korisnici se ne bi mogli prijaviti onako
kako uputstvo opisuje — a to je prva stvar koja se testira.

**`HistorijaStatusaRezervacije`.** Uputstvo (sekcija 7) traži audit trag na svakom
prelazu: ko, kada, razlog i opis. Polja `RazlogOtkazivanja`, `OtkazaoKorisnikId` i
`DatumOtkazivanja` na rezervaciji pokrivaju samo otkazivanje. Prelaz
`Pending → Confirmed` ili `Confirmed → Completed` nije imao gdje da se zapiše.

**`StanjeOpreme`.** Specifikacija traži zalihe opreme po poslovnici i da se oprema
koje nema na stanju ne nudi pri kreiranju rezervacije. Bez tabele to nema gdje živjeti.

**`KodZaResetLozinke`.** Dodatak A.3 uputstva traži da reset kodovi imaju definisan
rok isteka i da se ne čuvaju u čitljivom obliku. Zato entitet čuva `KodHash`, a ne
sam kod.

Uz njih, nekoliko sitnijih odluka gdje dokumenti nisu bili precizni:

- `Primopredaja.NivoGoriva` je `int` — procenat od 0 do 100.
- `Refund.Status` koristi postojeći `StatusPlacanja` enum umjesto novog. Stripe refund
  prolazi kroz ista stanja, pa novi enum ne bi rekao ništa novo.
- `Cjenovnik` je dobio `Naziv`, npr. „Ljetna sezona". Sezonski red bez oznake nema
  šta prikazati u CRUD formi.

### Jedna EF zamka koju smo izbjegli

U cijelom modelu se **ne koristi `HasDefaultValue`** ni na jednom bool ni decimal polju.

Razlog je suptilan i lako ga je previdjeti. EF ne šalje vrijednost koja je jednaka
CLR defaultu — za `bool` je to `false`, za `decimal` je `0`. Ako se na koloni `Aktivan`
postavi `HasDefaultValue(true)`, a zatim se upiše korisnik sa `Aktivan = false`, EF tu
vrijednost neće poslati, baza će primijeniti svoj default, i u bazi će završiti `true`.
Deaktivacija tiho ne radi.

Umjesto toga default vrijednosti stoje kao inicijalizatori u C# klasama:

```csharp
public bool Aktivan { get; set; } = true;
```

### Seed podaci

Seed je **runtime, a ne `HasData`**. Razlog je obim: oko tisuću zapisa sa
medjusobnim vezama i datumima racunatim od danasnjeg dana. Uputstvo (sekcija 3.1)
izricito dozvoljava da se podaci kreiraju pri pokretanju aplikacije.

Zivi u `SunnyRides.Services/Database/Seed/`, podijeljen u vise dijelova jedne
`partial` klase: sifrarnici, korisnici, flota, poslovanje, placanja i ostalo.
`Program.cs` ga poziva nakon `MigrateAsync()`, pa se aplikacija podize sa
`docker compose up --build` bez ijedne rucne komande.

Sjeme slucajnog generatora je fiksno (`220182`), pa svako pokretanje daje isti
raspored podataka. Seeder na pocetku provjerava postoji li ijedan korisnik i
preskace posao ako baza vec nije prazna.

Sta se upisuje:

| Entitet | Koliko |
|---|---|
| Korisnici | 20 (4 osoblje, 16 klijenata) |
| Vozila | 35, svako sa slikom i thumbnailom |
| Rezervacije | 95, kroz sest mjeseci unazad i dva mjeseca unaprijed, u sva cetiri statusa |
| Placanja | 92 |
| Refundi | 87 |
| Primopredaje | 160 (80 zavrsenih najmova, izdavanje i povrat) |
| Recenzije | 57 |
| Notifikacije | 298 |
| Historija pretraga | 140 |

Tri stvari u seedu nisu slucajne nego namjerne:

**Rezervacije se ne preklapaju.** Generisu se po vozilu, redom kroz vrijeme, sa
razmakom izmedju termina. Da nije tako, kalendar flote bi vec prvog dana prikazivao
nemoguce stanje.

**Klijent dobija samo vozila koja smije voziti.** Rezervacija se ne dodjeljuje
klijentu cija dozvola ne pokriva kategoriju vozila. Klijenti sa dozvolom u statusu
`NaCekanju` ili `Odbijena` nemaju nijednu rezervaciju - kao sto ni u stvarnosti ne
bi mogli rezervisati.

**Raspodjela kategorija je namjerno neravnomjerna.** Sest modela trazi A1, dva A,
dva B. Zbog toga se u pretrazi stvarno vidi razlika kad se prijavi klijent sa
drugom dozvolom; da su svi modeli u istoj kategoriji, filtriranje se ne bi imalo
na cemu pokazati.

Lozinke se hashiraju BCrypt-om jednom, pri pokretanju seeda, i taj isti algoritam
koristi prijava (`BCrypt.Verify`) - formati se poklapaju po konstrukciji.

### Design-time factory

`SunnyRidesDbContextFactory` postoji zbog jednog konkretnog problema: bez nje bi
`dotnet ef` radi migracija podizao cijeli API host, a time bi se pri svakoj komandi
izvrsio i kod iza `builder.Build()` - ukljucujuci migriranje i seed. Sa njom EF
dobije samo `DbContext` i nista vise.

Ista klasa cita `CONNECTION_STRING` iz environment varijable, a ako je nema, trazi
`.env` penjuci se od trenutnog foldera prema korijenu repozitorija.

### Enumi

Statusi su enumi u C#-u i `int` u bazi, mapirani eksplicitno kroz `HasConversion<int>()`.
Nigdje u kodu nema magic brojeva ni magic stringova za status.

```csharp
StatusRezervacije { Pending = 1, Confirmed = 2, Cancelled = 3, Completed = 4 }
StatusPlacanja    { Created = 1, Pending = 2, Succeeded = 3, Failed = 4, Canceled = 5 }
StatusDozvole     { NaCekanju = 1, Odobrena = 2, Odbijena = 3 }
TipPrimopredaje   { Izdavanje = 1, Povrat = 2 }
TipNotifikacije   { RezervacijaKreirana = 1, … , VoziloVraceno = 9 }
```

Nazivi rola su konstante u `SunnyRides.Model/Konstante/Uloge.cs`. Iste vrijednosti
koriste se u seed podacima i u `[Authorize(Roles = ...)]` atributima — kad se te dvije
stvari raziđu, autorizacija tiho pada na 403 i greška se traži satima.

---

## Prijava, token i uloge

Prijava ide na `POST /api/auth/login` i traži korisničko ime, ne email. Tako je jer
uputstvo (sekcija 5) traži da se pri pregledu rada može prijaviti sa `desktop`,
`mobile`, `administrator` i `uposlenik` — to su korisnička imena, ne adrese.

Servis traži korisnika, provjeri lozinku BCrypt-om i tek onda gleda stanje naloga:

```csharp
if (korisnik is null || !BCrypt.Net.BCrypt.Verify(request.Lozinka, korisnik.LozinkaHash))
    throw new BusinessException("Pogresno korisnicko ime ili lozinka.");
```

Jedna poruka pokriva oba slučaja namjerno. Da nepostojeći korisnik daje „korisnik ne
postoji", a postojeći sa krivom lozinkom „pogrešna lozinka", svako bi kroz formu za
prijavu mogao popisati koja korisnička imena u sistemu postoje. To je klasičan
**user enumeration** propust i košta ništa da se izbjegne.

Redoslijed je bitan i u drugom smjeru: provjera lozinke ide **prije** provjere
`Aktivan`. Da je obrnuto, poruka „nalog je deaktiviran" bi se dobila i bez tačne
lozinke, pa bi opet odavala postojanje naloga.

**Blokiran korisnik se namjerno može prijaviti.** Blokada u ovom sistemu znači da
korisnik ne može napraviti novu rezervaciju — ne da mu se oduzima pristup vlastitoj
historiji, računima i podacima. Deaktiviran nalog (`Aktivan = false`) je nešto drugo
i njemu se prijava odbija.

### Šta token nosi

| Claim | Vrijednost | Čemu služi |
|---|---|---|
| `sub` | `Korisnik.Id` | jedini izvor identiteta za servise |
| `jti` | GUID | identifikator tokena; po njemu se radi opoziv |
| `name` | korisničko ime | prikaz i logovanje |
| `ime`, `prezime` | — | da klijentska aplikacija ne mora odmah zvati `/api/auth/ja` |
| `role` | naziv uloge, može ih biti više | ulazi u `[Authorize(Roles = ...)]` |
| `exp` | `UtcNow + 120 min` | rok trajanja |

Nema `refresh` tokena. Dodatak A.2 uputstva to dozvoljava, a za sistem u kojem sesija
traje dva sata refresh mehanizam donosi drugu tabelu, drugu rutu i novu klasu grešaka
bez stvarne koristi. Klijentske aplikacije na 401 vode korisnika na ekran za prijavu.

Jedna zamka koju je `Microsoft.IdentityModel` lako postaviti: po defaultu se kratki
nazivi claimova prevode u duge URI oblike, pa se `role` pri čitanju pretvori u
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. Ako se to desi na
jednoj strani a ne na drugoj, autorizacija tiho pada na 403 i greška se traži satima.
Zato je mapiranje isključeno na obje strane — `DefaultOutboundClaimTypeMap.Clear()`
pri izdavanju i `MapInboundClaims = false` pri validaciji — a `RoleClaimType` je
eksplicitno postavljen na `"role"`.

`ClockSkew` je postavljen na nulu. Podrazumijevana vrijednost je pet minuta, što znači
da istekao token prolazi još pet minuta nakon `exp`. Za sistem sa jednim serverom to
nema smisla.

### Odjava

JWT je po prirodi bez stanja: jednom potpisan, važi do isteka roka i server o njemu
ne pamti ništa. Uputstvo ipak traži da odjava **invalidira token na serveru** —
brisanje tokena na uređaju nije dovoljno, jer token koji je neko presreo i dalje radi.

Rješenje je tabela `OpozvaniToken` i middleware koji svaki autentifikovan zahtjev
poredi sa njom po `jti`. Tabela ima indeks na `Jti`, a periodični posao u workeru
(faza 14) iz nje briše zapise kojima je rok ionako istekao, da ne raste beskonačno.

Redoslijed u `Program.cs` nije proizvoljan:

```csharp
app.UseAuthentication();                       // popuni HttpContext.User iz tokena
app.UseMiddleware<OpozvaniTokenMiddleware>();  // tek sad zna koji je jti
app.UseAuthorization();                        // tek sad provjerava uloge
```

Da provjera opoziva ide prije autentifikacije, ne bi imala šta čitati — `User` bi bio
prazan, `jti` `null`, i opozvan token bi prošao.

Ponovna odjava istim tokenom ne baca grešku nego tiho izlazi. Operacija je time
idempotentna: dva klika na „Odjavi se" daju isti rezultat kao jedan.

### Gdje stoji `[Authorize]`

Na `BaseController`, ne na svakom kontroleru ponaosob:

```csharp
[ApiController]
[Authorize]
public abstract class BaseController<TModel, TSearch> : ControllerBase
```

Time je zaštita podrazumijevano stanje. Svaki novi kontroler koji naslijedi bazu
zaštićen je prije nego u njemu bude napisana ijedna linija, a otvaranje endpointa
traži svjestan potez. `[AllowAnonymous]` u cijelom projektu postoji na tačno dva
mjesta: `login` i `register`.

Šifrarnici nose `[Authorize(Roles = Uloge.Administrator)]` na nivou kontrolera, jer
je održavanje šifrarnika administratorski posao. Kad klijentskoj aplikaciji zatreba
lista država za formu, čitaće je kroz endpoint tog modula — tamo se šifrarnik samo
čita, ovdje se i mijenja.

### Ko čita identitet

`ICurrentUserService` je jedini način na koji servis smije saznati ko poziva
operaciju, i sve čita iz `ClaimsPrincipal`:

```csharp
public int? KorisnikId =>
    int.TryParse(Korisnik?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
```

Nijedna vrijednost ne dolazi iz rute, query stringa ni tijela zahtjeva. Da `KorisnikId`
stiže kao parametar, svako bi mogao poslati tuđi identifikator i raditi nad tuđim
rezervacijama — a endpoint bi izgledao savršeno normalno.

Interfejs `ITokenService` živi u servisnom sloju, a implementacija u API projektu.
JWT je transportna stvar i tamo je već konfigurisana validacija; ovako servisni sloj
ostaje bez ijedne zavisnosti prema ASP.NET Core-u.

### Testovi kojima je faza zatvorena

| Test | Očekivano | Dobiveno |
|---|---|---|
| Prijava `administrator` / `test` | token + uloga Administrator | ✅ |
| `GET /api/drzave` bez tokena | 401 | ✅ |
| `POST /api/drzave` sa klijentskim tokenom | 403 | ✅ |
| `GET /api/drzave` sa admin tokenom | 200 | ✅ |
| Odjava, pa isti token ponovo | 401 | ✅ |

> Test za 403 je u fazi 7 prebačen sa `GET` na `POST`. Razlog je opisan u sekciji o
> šifrarnicima: čitanje šifrarnika otvoreno je svakom prijavljenom korisniku, jer ga
> mobilna aplikacija treba za padajuće liste, dok je izmjena ostala administratorska.
> Provjera uloge time nije oslabljena nego pomjerena tamo gdje stvarno pripada.

---

## Šifrarnici

Jedanaest referentnih tabela — države, gradovi, poslovnice, tipovi vozila, marke,
modeli, tipovi goriva, kategorije dozvola, pravila kategorija, vrste opreme i paketi
osiguranja — dijeli isti skelet: DTO, insert i update zahtjev, search objekt, servis
i kontroler. Svaki od njih ima **pet do sedam linija vlastitog koda**; ostalo dolazi
iz `BaseCRUDService` i `SifrarnikController`. To je jedina stvarna korist od
generičkih baznih klasa i razlog zašto su pisane prije nego ijedan konkretan servis.

### Ko smije šta

Čitanje i pisanje nisu jednako zaštićeni, i to je namjerno.

```csharp
public abstract class SifrarnikController<TModel, TSearch, TInsert, TUpdate>
    : BaseCRUDController<TModel, TSearch, TInsert, TUpdate>
{
    [Authorize(Roles = Uloge.Administrator)]
    public override Task<TModel> InsertAsync(...)
```

`GET` nasljeđuje `[Authorize]` sa `BaseController`, pa ga smije svaki prijavljen
korisnik. `POST`, `PUT` i `DELETE` traže ulogu `Administrator`.

Razlog je praktičan: klijent u mobilnoj aplikaciji bira poslovnicu preuzimanja, tip
vozila i marku, i te liste mora odnekud dobiti. Kad bi cijeli šifrarnik bio
zatvoren za administratora, svaki bi ekran pretrage trebao vlastiti paralelni
endpoint sa istim podacima — dvije rute nad istom tabelom, koje se s vremenom
raziđu. Sadržaj šifrarnika je ionako javan podatak agencije, isti za sve korisnike.

Ono što **nije** javno je pravo da se taj sadržaj mijenja, i to je zaključano na
administratora. Specifikacija kaže da modul referentnih podataka ne vidi ni
uposlenik, a to je upravo ovo: pristup formama za unos i izmjenu, ne pristup listi.

Atributi stoje na `SifrarnikController`, a ne na svakom od jedanaest kontrolera.
Razlog je isti kao kod `[Authorize]` na `BaseController` — ono što se piše jedanaest
puta, dvanaesti put se zaboravi.

### Strani ključevi

Zahtjev koji pokazuje na nepostojeći zapis ne smije doći do baze. Provjera je u
baznom servisu, kao metoda koju konkretni servisi zovu u `BeforeInsert` i
`BeforeUpdate`:

```csharp
protected async Task ObaveznoPostojiAsync<TStrani>(int id, string naziv, CancellationToken ct)
```

Bez nje bi `GradInsertRequest` sa `DrzavaId = 999` prošao kroz servis i pukao tek na
`SaveChangesAsync`, kao `DbUpdateException` iz SQL Servera — što `ExceptionFilter`
pretvara u **500 sa generičkom porukom**. Korisniku tada piše da je došlo do
neočekivane greške, a u stvari je samo odabrao stavku koje više nema.

Provjera baca `BusinessException`, dakle **400, a ne 404**. Ovo je razlika koja se na
odbrani zna pitati: 404 znači da traženi resurs ne postoji, a ovdje endpoint postoji
i radi — pogrešan je sadržaj zahtjeva. 404 bi klijentskoj aplikaciji rekao da je ruta
kriva i poslao je da traži problem na pogrešnom mjestu.

### Brisanje

Nijedan šifrarnik ne dopušta brisanje zapisa koji se koristi. Provjera je u
`BeforeDelete` svakog servisa, sa porukom koja imenuje i zapis i razlog:

> „Grad "Mostar" se ne može obrisati jer postoji 2 poslovnica u njemu."

Strani ključevi su u bazi postavljeni na `Restrict`, pa bi brisanje puklo i bez ove
provjere — ali bi puklo kao izuzetak iz baze, a uputstvo izričito traži jasnu poruku
umjesto EF izuzetka. `BaseCRUDService.DeleteAsync` ipak hvata i taj slučaj, kao
mrežu za vezu koju sam previdio: `DbUpdateException` pri brisanju postaje
`BusinessException` sa generičnijom, ali i dalje razumljivom porukom.

Redoslijed je, dakle: prvo `BeforeDelete` sa konkretnom porukom, pa `Restrict` u
bazi kao tvrda garancija, pa prevođenje izuzetka kao posljednja odbrana.

### Nazivi umjesto identifikatora

`GradDto` uz `DrzavaId` nosi i `DrzavaNaziv`, `ModelVozilaDto` nosi naziv marke, tipa,
goriva i oznaku kategorije. Bez toga bi lista modela prikazivala brojeve, a klijentska
aplikacija bi za svaki red radila dodatni poziv — klasičan N+1, samo preseljen na
mrežu umjesto u bazu.

Vrijednosti dolaze iz navigacija koje servis učitava kroz `AddInclude`, jednim
upitom sa `JOIN`-om. Mapster ih preslikava po eksplicitnoj konfiguraciji u
`MapsterKonfiguracija`, a ne po konvenciji — kad bi konvencija promašila naziv, polje
bi tiho ostalo prazno i lista bi prikazivala rupe. Include i mapiranje idu u paru:
ako se izbaci jedno, drugo prestane davati vrijednost.

### Dva pravila koja se ne vide iz modela

**Poslovnica.** Koordinate su opcione, ali idu u paru. Poslovnica sa samo latitudom
se na mapi ne može prikazati, a podatak izgleda kao da postoji.

**Vrsta opreme.** Cijena je ili po danu ili fiksna, nikad oboje i nikad nijedno.
`PricingService` u fazi 9 bira granu obračuna po tome koje je polje popunjeno; da su
obje popunjene, cijena bi zavisila od redoslijeda `if` grana u kodu. To je tačno
vrsta neodređenosti koju u obračunu ne smijemo imati, pa se odbija pri unosu.

**Kategorija dozvole.** Oznaka se normalizuje na velika slova prije upisa. Bez toga
bi „a1" i „A1" prošli kao dva zapisa, a kasnija provjera kategorija poredila bi
stringove koji se razlikuju samo veličinom slova.

### Duplikati i ono što se vidi u logu

Duplikat se ne provjerava upitom prije upisa nego se oslanja na jedinstveni indeks.
Razlog je konkurentnost: provjera pa upis su dvije radnje, a između njih stane tuđi
zahtjev sa istim nazivom. Indeks je jedina garancija koja to ne može propustiti.

`BaseService.SacuvajAsync` hvata `DbUpdateException`, prepoznaje SQL greške 2601 i
2627 i pretvara ih u `BusinessException` sa porukom koju servis sam definiše — pa
klijent dobije 400 i rečenicu, a ne 500.

Uz to je EF-ov događaj `SaveChangesFailed` spušten na `Debug`:

```csharp
options.ConfigureWarnings(w => w.Log((CoreEventId.SaveChangesFailed, LogLevel.Debug)));
```

Bez toga EF isti, potpuno očekivani ishod prijavljuje kao `Error` sa punim stack
traceom. Aplikacija je uredno odgovorila, ali log izgleda kao da je pukla — a onaj ko
rad pregleda gleda upravo taj log. Događaj se i dalje bilježi, samo na nivou koji
odgovara tome što jeste.

### Testovi kojima je faza zatvorena

| Test | Očekivano | Dobiveno |
|---|---|---|
| `GET` na svih jedanaest šifrarnika | 200 | ✅ |
| Paginacija: `pageSize=3` od 6 gradova | 3 zapisa, `totalCount` 6 | ✅ |
| Nazivi umjesto identifikatora na modelu | `CB125R: Honda / Motocikl / Benzin / A1` | ✅ |
| Pretraga po dijelu naziva | filtrira na bazi kroz `LIKE` | ✅ |
| `GET` kao klijent | 200 | ✅ |
| `POST` kao klijent | 403 | ✅ |
| Strani ključ koji ne postoji | 400 sa porukom, ne 500 | ✅ |
| Brisanje države koja ima gradove | 400 sa objašnjenjem | ✅ |
| Oprema sa obje cijene | 400 sa objašnjenjem | ✅ |
| Ciklus kreiraj → izmijeni → duplikat → obriši | 200, 200, 400, pa 404 | ✅ |

> **Šta ovaj test nije dokazao.** Ograničenje `PageSize` na 100 nije provjereno kako
> treba — `pageSize=5000` je vratio 6 zapisa, ali zato što gradova ukupno ima 6.
> Granica se ne može vidjeti dok neka tabela ne pređe stotinu redova. Provjerava se
> na donjoj granici istog `Math.Clamp` poziva: `pageSize=0` vraća jedan zapis, a ne
> nula i ne grešku. Puni dokaz dolazi u fazi 11, kad rezervacije dobiju endpoint —
> njih u seedu ima preko šezdeset, a pretraga bez filtera ih vraća sve.

---

## Flota i fotografije

### Šta vozilo nasljeđuje od modela

Vozilo je konkretan primjerak — jedna registarska oznaka, jedna kilometraža, jedna
poslovnica. Sve što je svojstvo **tipa** a ne primjerka živi na `ModelVozila`:
kubikaža, snaga, tip goriva i, najvažnije, **potrebna kategorija vozačke dozvole**.

Zato `VoziloInsertRequest` nema polje `KategorijaDozvoleId`. Da ga ima, dva primjerka
iste Honde CB125R mogla bi završiti sa različitim kategorijama — jedan zahtijeva A1,
drugi A — i filtriranje iz faze 10 davalo bi rezultate koje niko ne bi umio objasniti.
Ovako je kategorija upisana na jednom mjestu i vozilo je samo pokazuje.

`VoziloDto` ipak nosi `kategorijaDozvoleOznaka`, `kubikaza` i `markaNaziv`. To nije
dupliranje podatka nego pogodnost za prikaz: kartica u pretrazi mora pokazati sve to
odjednom, a bez toga bi klijent za svaki red morao dohvatiti i model — N+1, samo
preseljen sa baze na mrežu.

### Dva korijena za fajlove

Otpremljeni fajlovi žive u dva odvojena stabla i razlika među njima je sigurnosna,
ne organizaciona:

| Folder | Šta sadrži | Kako se dohvata |
|---|---|---|
| `uploads/` | fotografije vozila, slike obavijesti | statički, bez tokena |
| `privatno/` | fotografije vozačkih dozvola i štete | isključivo kroz endpoint sa provjerom vlasništva |

`app.UseStaticFiles` je konfigurisan da poslužuje **samo** javni korijen. Da su
osjetljivi fajlovi u istom stablu, taj jedan poziv bio bi dovoljan da fotografija
tuđe vozačke dozvole postane dostupna svakome ko pogodi putanju — bez ijedne greške
u kodu, samo zbog izbora foldera.

Razdvajanje je uvedeno u fazi 8, dok `privatno/` još stoji prazan. Namjerno prije
nego u njemu bude podataka: konvencija koja se uvodi kasnije znači premještanje
fajlova i ispravku putanja koje su već upisane u bazu.

Korijeni se pronalaze sami. U kontejneru je radni folder `/app`, a Compose u njega
montira `./uploads`; pri lokalnom `dotnet run` radni folder je `SunnyRides.API`, a
folder je jedan nivo iznad. Ista logika kao kod `.env` fajla — isti kod radi u oba
okruženja bez ijedne izmjene.

### Zašto se MIME tip provjerava po prvim bajtima

Ekstenzija fajla i `Content-Type` zaglavlje dolaze od klijenta i oboje se slobodno
falsifikuju. `virus.exe` preimenovan u `slika.jpg` prolazi svaku provjeru koja gleda
naziv. Prvi bajtovi su dio samog sadržaja i njih napadač ne može promijeniti a da
fajl ostane ono što tvrdi da jeste.

```
JPEG   FF D8 FF
PNG    89 50 4E 47 0D 0A 1A 0A
WEBP   "RIFF" ···· "WEBP"   (bajtovi 0-3 i 8-11)
```

Provjera je namjerno **dvostruka**. Potpis je prva, jeftina kapija. Druga je samo
učitavanje slike kroz ImageSharp: sadržaj koji ima ispravna prva tri bajta a
pokvarenu strukturu tu pada, i to prije nego išta dodirne disk. Test je to i
potvrdio — tekstualni fajl preimenovan u `.jpg` odbijen je sa 400, a u folderu
vozila nije ostao nijedan trag.

### Šta se dešava pri uploadu

1. Kontroler prima `multipart/form-data` i prosljeđuje **samo tok bajtova i dužinu**.
   Ne otvara fajl, ne prepoznaje format, ne računa putanju.
2. Servis provjerava veličinu (najviše 5 MB), pa potpis.
3. Original se smanjuje na najviše 1600 px po dužoj stranici i snima kao JPEG.
4. Thumbnail 200×150 se siječe na tačan omjer, da kartice u listi budu iste visine.
5. U bazu ide **putanja**, nikad sadržaj.

Treći korak nije kozmetika. Fotografija sa telefona zna biti dvanaest megabajta, a na
ekranu se nikad ne vidi više od par stotina piksela — čuvanje originala u punoj
veličini samo puni disk i usporava galeriju.

Sadržaj ide kao multipart, nikad kao base64 u JSON-u. Base64 povećava prenos za
trećinu i cijeli fajl drži u memoriji kao string.

### Redoslijed diska i baze

Dvije operacije, dva suprotna redoslijeda, i oba su namjerna.

**Pri dodavanju** fajl ide prvi, jer se putanja koju treba upisati zna tek kad je
snimljen. Ako `SaveChangesAsync` poslije padne, fajl se briše u `catch` bloku —
inače bi ostao zauvijek, bez ijednog zapisa koji na njega pokazuje.

**Pri brisanju** je obrnuto: prvo baza, pa disk. Suvišan fajl na disku je smeće;
zapis koji pokazuje na fajl kojeg više nema je pokvaren podatak koji će puknuti pri
prvom otvaranju galerije. Od dva loša ishoda bira se manji.

Isto vrijedi i za brisanje vozila: putanje se pročitaju u `BeforeDelete`, dok se još
zna gdje su, a fajlovi se uklanjaju nakon što je zapis nestao iz baze. Slike se u
bazi brišu kaskadno, ali kaskada ne zna za disk.

### Glavna slika je uvijek tačno jedna

Prva otpremljena slika automatski postaje glavna — da vozilo nikad ne ostane bez
thumbnaila u listi samo zato što je neko zaboravio kliknuti. Prebacivanje oznake
skida je sa svih ostalih i postavlja na odabranu **u istom** `SaveChangesAsync`
pozivu, pa ne postoji trenutak u kojem su dvije glavne ili nijedna. Brisanje glavne
je dodjeljuje sljedećoj po redoslijedu.

### Vozilo se ne briše, nego deaktivira

`DELETE /api/vozila/{id}` na vozilu koje ima rezervacije vraća 400 sa porukom koja
kaže šta uraditi umjesto toga:

> „Vozilo "A18-O-178" se ne može obrisati jer postoje rezervacije (2). Deaktivirajte
> ga umjesto brisanja."

Historija najmova mora ostati čitava radi izvještaja. Deaktivirano vozilo nestaje iz
pretrage i kalendara flote, a prošlost ostaje netaknuta. Vozilo koje nikad nije bilo
izdato se briše normalno — zajedno sa svojim fotografijama.

### Testovi kojima je korak zatvoren

| Test | Očekivano | Dobiveno |
|---|---|---|
| Lista vozila sa thumbnailom i nazivima | 35 vozila, svako sa markom, poslovnicom i kategorijom | ✅ |
| Thumbnail se poslužuje statički | 200, `image/jpeg` | ✅ |
| Filteri po tipu, cijeni i statusu | 20 / 21 / 2 | ✅ |
| Klijent čita flotu, ne mijenja je | 200 / 403 | ✅ |
| Dnevna tarifa niža od satne | 400 sa objašnjenjem | ✅ |
| Brisanje vozila sa rezervacijama | 400, uputa da se deaktivira | ✅ |
| Upload: prva slika postaje glavna | `jeGlavna = true` | ✅ |
| Upload: druga ne preuzima oznaku | `jeGlavna = false`, redoslijed 1 | ✅ |
| Prebacivanje glavne | thumbnail vozila se mijenja | ✅ |
| Tekstualni fajl sa `.jpg` ekstenzijom | 400, **nijedan fajl na disku** | ✅ |
| Brisanje glavne promoviše sljedeću | thumbnail se vraća na prvu | ✅ |
| Brisanje vozila čisti disk | 0 preostalih fajlova | ✅ |

---

## Blokade i cjenovnik

### Blokada je, za dostupnost, isto što i rezervacija

Blokada vozila je period u kojem vozilo nije za najam — servis, kvar, sezonsko
povlačenje iz flote. Sa stanovišta pretrage, to je ista činjenica kao potvrđena
rezervacija: vozilo je zauzeto. Zato će ih `AvailabilityService` u fazi 9 čitati
istim uslovom, a ne kao dva odvojena slučaja.

Cijeli modul je za osoblje. Klijent blokade ne vidi ni kao listu — za njega je
blokirano vozilo jednostavno vozilo koje se u pretrazi ne pojavljuje, a razlog
blokade je interni podatak agencije.

**Ko je blokadu evidentirao čita se iz tokena.** `BlokadaVozilaInsertRequest`
namjerno nema polje `KreiraoKorisnikId`. Da ga ima, svaki uposlenik mogao bi blokadu
pripisati kolegi — a blokada je audit zapis, ono „ko je i zašto vozilo povukao iz
flote". Isto pravilo koje važi za `userId` pri rezervaciji važi i ovdje, samo je
ovdje lakše previdjeti jer polje izgleda kao običan strani ključ.

Pri izmjeni se to polje **ne dira**. Blokada ostaje pripisana onome ko ju je unio i
kad je kasnije neko drugi ispravi — inače bi audit trag pokazivao posljednjeg
urednika umjesto autora.

### Šta u blokadama namjerno nije provjereno

Dvije blokade istog vozila smiju se preklapati. To nije previd: obje znače da je
vozilo nedostupno, a provjera dostupnosti je unija perioda, ne raspodjela. Pravilo
koje bi to zabranilo ne bi štitilo ništa, a odbijalo bi legitiman unos — produženje
servisa koji se preklapa sa već upisanim terminom.

Provjerava se ono što jeste greška: kraj prije početka i trajanje duže od godinu
dana, jer je to gotovo sigurno promašen datum pri unosu.

> ⬜ **Šta nedostaje.** Plan izrade traži da unos blokade prikaže sve `Confirmed`
> rezervacije koje se s njom preklapaju, da uposlenik odluči hoće li ponuditi
> zamjensko vozilo ili otkazati uz puni povrat. Taj uslov preklapanja pripada
> `AvailabilityService`-u, a specifikacija izričito traži da za njega postoji **samo
> jedna** implementacija. Da je napišem ovdje, dobio bih drugu kopiju istog pravila —
> tačno grešku pred kojom uputstvo upozorava. Dolazi u fazi 9, uz endpoint koji vraća
> pogođene rezervacije.

### Zašto se dvije tarife ne smiju preklapati

Cjenovnik nosi sezonski množilac i pragove popusta za jedan model vozila u jednom
periodu. Jedina tvrda provjera je da se dva perioda za isti model ne preklapaju.

Razlog je konkretan: da smiju, `PricingService` bi pri obračunu morao birati između
dva množioca, a taj izbor nigdje nije definisan. Cijena bi zavisila od redoslijeda
zapisa u bazi — a to je vrsta neodređenosti koju u novcu ne smijemo imati. Zato
`VazeciAsync` smije koristiti `FirstOrDefault`: to nije „bilo koja od nekoliko" nego
posljedica pravila da ih više od jedne ne može biti.

Uz to ide i pravilo da popust na dužem pragu ne smije biti manji od popusta na
kraćem. Inače bi klijentu bilo isplativije rezervisati kraće, što je suprotno svrsi
popusta.

Pragovi su podatak, ne konstanta u kodu. Ekran sa detaljima vozila mora prikazati
tačno one pragove koji se stvarno primjenjuju pri obračunu, a to je moguće samo ako
oba čitaju isto mjesto.

### Keš je na servisnom nivou, ne kao polje u klasi

Cjenovnik se čita pri svakoj pretrazi i pri svakom obračunu cijene, a mijenja se
nekoliko puta godišnje — udžbenički slučaj za keširanje. Ide kroz `IMemoryCache`,
ključ je `cjenovnik:model:{id}`.

Razlika u odnosu na `Dictionary` polje u servisu nije kozmetička. Servis je `Scoped`,
pa bi `Dictionary` živio tačno jedan zahtjev i ne bi uštedio ništa. Da je statički,
ništa ga ne bi čistilo ni ograničavalo — rastao bi dok aplikacija radi i ne bi znao
kad je podatak zastario.

Keš se poništava pri svakom upisu za taj model, i to i kad brisanje na kraju ne
uspije: hladan keš je jedan upit više, a nešto što je ostalo u kešu a više ne postoji
u bazi je pogrešna cijena. Uz to ima i rok od petnaest minuta, kao mrežu za slučaj da
se podatak promijeni mimo servisa — migracijom ili ručnim upitom u bazi.

### Odgovor na upis mora izgledati kao odgovor na dohvat

Test blokade otkrio je grešku koja se u šifrarnicima nije vidjela: `POST` i `PUT` su
odgovor gradili od entiteta koji je upravo napravljen ili izmijenjen, a njemu
navigacije nisu učitane. Identifikatori ispravni, nazivi prazni.

U `MarkaDto` nema nijednog polja iz navigacije, pa se to nije primijetilo. Prvi
entitet sa vezama odmah je pokazao rupu — `voziloRegistarskaOznaka` i
`kreiraoKorisnikIme` vratili su se prazni.

Posljedica nije kozmetička. Uputstvo traži da se poslije spašavanja korisnik vrati na
listu sa novim zapisom na vrhu, bez ručnog osvježavanja; taj red bi ostao prazan
tamo gdje treba pisati naziv poslovnice i ime uposlenika.

Popravka je u `BaseCRUDService`: nakon upisa se zapis pročita istim putem kojim ide i
običan dohvat po identifikatoru.

```csharp
var id = (int)Context.Entry(entitet).Property("Id").CurrentValue!;
return await GetByIdAsync(id, ct);
```

Košta jedan `SELECT` po upisu. Alternativa je da svaki servis ručno puni navigacije
poslije upisa — što se zaboravi na prvom sljedećem entitetu. Ovako `POST`, `PUT` i
`GET` vraćaju doslovno isti oblik zapisa, pa se klijentska aplikacija ne mora
ponašati drugačije prema odgovoru na spašavanje nego prema odgovoru na dohvat.

### Testovi kojima je korak zatvoren

| Test | Očekivano | Dobiveno |
|---|---|---|
| Cjenovnik iz seeda, šest sezona po modelu | glavna sezona ×1,30, vansezona ×0,85 | ✅ |
| Koja tarifa važi danas | jedna, sa pragovima 3 d/5 % i 7 d/10 % | ✅ |
| Tarifa koja se preklapa sa postojećom | 400, imenuje tarifu i njen period | ✅ |
| Veći popust na kraćem pragu | 400 sa objašnjenjem | ✅ |
| Cjenovnik: uposlenik čita, ne mijenja | 200 / 403 | ✅ |
| Blokade: klijent ih ne vidi uopće | 403 / 200 | ✅ |
| Unos blokade, autor iz tokena | `Emina Hodzic (id 3)`, nije iz zahtjeva | ✅ |
| Kraj blokade prije početka | 400 | ✅ |
| Blokada duža od godinu dana | 400 | ✅ |
| Pretraga po periodu koji se preklapa | 1, odnosno 0 za drugi period | ✅ |
| Izmjena od strane administratora | autor ostaje uposlenik | ✅ |
| `POST` vraća nazive iz navigacija | registracija, model, poslovnica, ime | ✅ |

---

## Obračun cijene

Cijena se računa na jednom mjestu i taj put je jedini. Zovu ga dvije stvari i obje
moraju dobiti isti broj: pregled cijene koji klijent vidi prije rezervacije, i samo
kreiranje rezervacije. Da postoje dvije implementacije, klijent bi vidio jednu cijenu
a platio drugu — i to bi se otkrilo tek kad neko uporedi račun sa ekranom.

### Tri sloja, i zašto baš tako

| Klasa | Odgovornost | Zna za bazu |
|---|---|---|
| `TrajanjeNajma` | koliko sati ili dana se naplaćuje | ne |
| `ObracunCijene` | sastavlja razradu od gotovih brojeva | ne |
| `PricingService` | učita vozilo, sezonu, opremu, osiguranje | da |

Podjela nije estetska. Kontrolni primjeri iz uputstva — 24 h 30 min, 25 h, 48 h,
49 h — testiraju se **bez baze, bez mokova i bez konteksta**, kao obična funkcija.
Test koji prolazi zato što je mok podešen da vrati očekivani rezultat ne dokazuje
ništa; ovaj računa stvarnu aritmetiku.

### Pravilo trajanja

```
trajanje ≤ 6 h        →  satna tarifa, započeti sat se računa cijeli
6 h < trajanje ≤ 24 h →  dnevna tarifa, jedan dan
trajanje > 24 h       →  puni dani + tolerancija 59 minuta
```

Tolerancija je ono što se najlakše pogriješi. Vraćanje vozila deset minuta poslije
roka ne smije klijenta koštati cijeli dodatni dan; sat i više već smije. Zato:

| Trajanje | Ostatak preko punih dana | Naplaćeno |
|---|---|---|
| 24 h 30 min | 30 min | 1 dan |
| 24 h 59 min | 59 min | 1 dan |
| 25 h | 60 min | 2 dana |
| 48 h | 0 | 2 dana |
| 49 h | 60 min | 3 dana |

Granica je **strogo veće od** 59 minuta, ne veće ili jednako. Tačno 59 minuta je još
unutar tolerancije.

### Tri odluke koje uputstvo ne precizira

**Započeti sat se računa cijeli.** Inače bi najam od 61 minute koštao kao najam od
jednog sata. Isti princip kao na parkingu.

**Sezonski množilac djeluje prije popusta.** Popust je popust na cijenu koja se
stvarno naplaćuje, pa se računa od iznosa koji već nosi sezonu:

```
osnovica    = dnevnaTarifa × dani
saSezonom   = osnovica × množilac
popust      = saSezonom × procenat
iznosNajma  = saSezonom − popust
```

Obrnutim redoslijedom bi se popust računao od cijene koja se nikad ne naplaćuje.

**Najam po satu se za opremu i osiguranje računa kao jedan dan.** Inače bi GPS na
petosatnom najmu bio besplatan, jer je broj dana nula.

### Odakle dolaze pragovi popusta

Iz cjenovnika, ne iz koda. `PopustPrag1`, `PopustProcenat1`, `PopustPrag2` i
`PopustProcenat2` su kolone u tabeli `Cjenovnik`, pa se sezonska akcija mijenja
unosom podatka umjesto ponovnim prevođenjem. Gleda se prvo viši prag, da najam od
deset dana dobije popust za sedam a ne za tri dana.

Ako za datum preuzimanja nema definisane sezone, koristi se podrazumijevana politika
3 dana/5 % i 7 dana/10 % — iste vrijednosti koje stoje u seed cjenovniku. To je
zaštita da najam od sedam dana ne ostane bez popusta samo zato što neko nije unio
tarifu za taj period.

**Sezona se bira po datumu preuzimanja, ne po današnjem danu.** Rezervacija
napravljena u maju za termin u julu plaća se po ljetnoj tarifi. Cjenovnik smije
nadjačati i samu tarifu vozila; kad je ne navede, važi tarifa upisana na primjerku.

### Zaokruživanje

Svaka stavka se zaokružuje na dvije decimale **posebno**, a ukupan iznos je zbir već
zaokruženih stavki. Zbog toga se prikazana razrada uvijek sabira u prikazani ukupni
iznos. Da se zaokružuje samo na kraju, klijent bi vidio listu brojeva koja se ne
sabira u ono što plaća — a to je prva stvar koju čovjek provjeri kad mu se račun
učini previsokim.

### Šta u zahtjevu namjerno ne postoji

`ObracunCijeneRequest` nema nijedno polje sa iznosom. Klijent kaže koje vozilo, za
koji period, koju opremu i koji paket osiguranja — sve što ima cijenu server čita iz
baze. Da iznos stiže izvana, klijent bi mogao rezervisati po cijeni koju sam odredi.

Isto vrijedi za `StavkaOpremeRequest`: šalje se šta i koliko, nikad po kojoj cijeni.

### Testovi kojima je korak zatvoren

31 test, svi prolaze. Trajanje pokriva granice — tačno 6 h, 6 h 1 min, tačno 24 h,
24 h 59 min, 25 h, 48 h 59 min naspram 49 h — te datum vraćanja prije preuzimanja.
Obračun pokriva pragove popusta, redoslijed množioca i popusta, opremu po danu
naspram fiksne cijene, opremu na satnom najmu, osiguranje, depozit, zbir razrade i
zaokruživanje na iznosu koji pada tačno na polovinu (5,265 → 5,27).

> ⬜ **Šta ovi testovi ne pokrivaju.** `PricingService` sam po sebi nema testove —
> njegov posao je učitavanje iz baze, a to bi tražilo test bazu ili mokove.
> Provjereno je kroz API: ista rezervacija u julu i u novembru daje različit iznos,
> jer se povlači različita sezona.

---

## Kome se vjeruje: klijent naspram servera

> ⬜ Popunjava se u fazama 9–12.

Ovo je najvažnija sekcija dokumenta. Za svaku operaciju koja prima podatke izvana
mora biti jasno koja vrijednost se prihvata kakva jeste, a koju server sam izračuna
ili učita iz baze.

Pravilo je jednostavno: **klijentu se vjeruje šta hoće, ali ne i koliko to košta.**

### Kreiranje rezervacije

| Podatak | Odakle | Napomena |
|---|---|---|
| `VoziloId` | klijent | provjerava se da vozilo postoji i da je aktivno |
| `DatumOd`, `DatumDo` | klijent | provjerava se da je `DatumDo > DatumOd` |
| `PaketOsiguranjaId` | klijent | opcionalno |
| stavke opreme | klijent | količina se provjerava prema `StanjeOpreme` |
| `KorisnikId` | **server** | iz JWT tokena, nikad iz rute ili tijela zahtjeva |
| `UkupanIznos` | **server** | `PricingService`; klijent ga ne šalje |
| `IznosDepozita` | **server** | iz `Vozilo.IznosDepozita` |
| `IznosPopusta` | **server** | `PricingService` |
| `Status` | **server** | uvijek `Pending` |
| `IsPaid` | **server** | uvijek `false`; mijenja se samo kroz potvrdu plaćanja |
| `DrziDo` | **server** | `UtcNow + 15 min` |
| `Broj` | **server** | generisan |
| `CijenaPoJedinici` na stavci | **server** | iz cjenovnika u trenutku kreiranja, da kasnija izmjena tarife ne promijeni staru rezervaciju |

### Registracija

| Podatak | Odakle | Napomena |
|---|---|---|
| `KorisnickoIme` | klijent | jedinstveno; provjerava se prije upisa i štiti indeksom |
| `Ime`, `Prezime`, `Email`, `Telefon` | klijent | format se validira anotacijama |
| `DatumRodjenja` | klijent | server iz njega računa godine |
| `Lozinka` | klijent | server je odmah hashira, čist tekst se nigdje ne čuva |
| `LozinkaHash` | **server** | BCrypt |
| **uloga** | **server** | uvijek `Klijent`, bez izuzetka |
| `Aktivan` | **server** | `true` |
| `Blokiran` | **server** | `false` |
| `DatumRegistracije` | **server** | `UtcNow` |

Ključno je šta u `RegisterRequest` **ne postoji**: nema polja `Role`, `RoleId` ni
`IsAdmin`. Da postoji bilo koje od njih, klijent bi pri registraciji sam sebi mogao
dodijeliti administratorska prava — a zahtjev bi izgledao potpuno legitimno. Uloga se
ne prima nego se u servisu učita iz baze i zakači na novog korisnika:

```csharp
var ulogaKlijent = await _context.Role.FirstOrDefaultAsync(x => x.Naziv == Uloge.Klijent, ct)
    ?? throw new BusinessException("Uloga Klijent ne postoji u sistemu.");
```

Registracija je dozvoljena od 16. godine; granica se računa na serveru iz
`DatumRodjenja`, ne prima se kao broj godina od klijenta.

---

## Redoslijed provjera

> ⬜ Popunjava se u fazama 9–13.

Za svaku operaciju treba znati tačan redoslijed provjera — do prve koja zahtjev
odbije — i koji status klijent tad dobija.

### Kreiranje rezervacije

Planirani redoslijed, sve unutar jedne transakcije:

1. `DatumDo > DatumOd` → 400
2. Dozvola postoji i status je `Odobrena` → 400
3. `Dozvola.DatumIsteka > rezervacija.DatumOd` → 400
   Bitno: provjera ide **na datum preuzimanja**, ne na današnji dan. Dozvola koja
   ističe za tri dana ne pokriva najam za dvije sedmice.
4. Kategorije dozvole pokrivaju kategoriju vozila → 400, sa konkretnom porukom u
   stilu *„Za ovo vozilo je potrebna kategorija A, a vaša dozvola pokriva A1 i B."*
5. Vozilo je aktivno → 400
6. Dostupnost kroz `AvailabilityService` → 400
7. Obračun cijene kroz `PricingService`
8. Upis rezervacije i stavki opreme
9. Commit
10. Objava poruke `rezervacija.kreirana` na RabbitMQ — **poslije** commita, nikad unutar
    transakcije

### Potvrda plaćanja

⬜ Faza 12.

---

## Šta se dešava kad dva zahtjeva stignu istovremeno

> ⬜ Popunjava se u fazama 9, 11 i 12.

Ovdje se za svaki scenarij mora znati **koji konkretno mehanizam** ga štiti: provjera
prije upisa, jedinstveno ograničenje u bazi, eksplicitna transakcija, concurrency
token, zaključavanje reda ili uslovni upis. Gdje mehanizma nema, to i piše.

| Scenarij | Čime je riješeno | |
|---|---|---|
| Dva zahtjeva za isto vozilo i preklapajući termin | eksplicitna transakcija sa provjerom dostupnosti unutar nje | ⬜ |
| Dvostruko slanje iste forme | jedinstveni indeks `(KorisnikId, VoziloId, DatumOd)` | 🟡 indeks postoji, servis ga još ne hvata |
| Dva uspješna plaćanja iste rezervacije | filtrirani jedinstveni indeks gdje `Status = 3` | 🟡 indeks postoji, servis još ne postoji |
| Otkazivanje i potvrda plaćanja istovremeno | — | ⬜ |
| Dvije recenzije istog najma | jedinstveni indeks `(KorisnikId, RezervacijaId)` | 🟡 indeks postoji |
| Dvije primopredaje istog tipa | jedinstveni indeks `(RezervacijaId, Tip)` | 🟡 indeks postoji |
| Isti Stripe događaj dvaput | jedinstveni indeks na `ProviderEventId` | 🟡 indeks postoji |

Razlog za 🟡 umjesto ✅ vrijedi razumjeti. Jedinstveni indeks stvarno **sprječava**
duplikat i onda kad provjera u kodu ne uhvati trku — baza jednostavno odbije drugi
upis. Ali odbije ga kroz `DbUpdateException`, koji, ako ga servis ne uhvati, do
klijenta stigne kao 500 sa nerazumljivom porukom.

Dakle: podatak je zaštićen, korisničko iskustvo nije. Dok servis to ne obradi i ne
vrati razumnu poruku, pošteno je reći da stvar nije završena.

---

## Idempotentnost

> ⬜ Faza 12.

| Operacija | Kako | |
|---|---|---|
| `POST /api/placanja/{id}/confirm` | ako je plaćanje već `Succeeded`, vraća 200 i **ne ponavlja efekte** — bez drugog emaila, bez druge notifikacije | ⬜ |
| Kreiranje payment intenta | ako postoji otvoren `Created`/`Pending` intent, vraća postojeći umjesto novog | ⬜ |
| Stripe webhook | `ObradjeniWebhookEvent.ProviderEventId` je jedinstven; postojanje zapisa znači da je događaj već obrađen | ⬜ |
| Idempotency key prema Stripe-u | `rez-{id}-v{verzija}` | ⬜ |

---

## Životni ciklus rezervacije

> ⬜ Implementira se u fazi 11.

Rezervacija ima **tačno četiri statusa**. Peti se ne dodaje — uputstvo to propisuje.

| Iz | U | Kad |
|---|---|---|
| `Pending` | `Confirmed` | plaćanje verifikovano na serveru |
| `Pending` | `Cancelled` | isteklo držanje ili klijent odustao — bez povrata |
| `Confirmed` | `Cancelled` | otkazivanje prije preuzimanja — povrat po politici |
| `Confirmed` | `Completed` | evidentiran povrat vozila |
| `Completed` | — | terminalno |
| `Cancelled` | — | terminalno |

Ono što na prvi pogled fali jeste status za „vozilo je izdato". Njega namjerno nema.
**Izdavanje vozila ne mijenja status rezervacije** — ona ostaje `Confirmed`. Fizički
tok vozila prati se kroz zapise `Primopredaja` tipa `Izdavanje` i `Povrat`, upravo
zato što uputstvo propisuje četiri statusa.

Svi prelazi žive u `RezervacijaStateMachine`. Kontroleri je pozivaju i nikad ne diraju
`Status` direktno. Svaki prelaz upisuje zapis u `HistorijaStatusaRezervacije` sa
poljima `StatusIz`, `StatusU`, `Razlog`, `Opis`, `IzvrsioKorisnikId` i `DatumVrijeme`.
Nedozvoljen prelaz baca `BusinessException` sa objašnjenjem.

---

## Cijena

> ⬜ Faza 9. Jedino mjesto u sistemu gdje se računa cijena je `PricingService`.

```
trajanje = DatumDo - DatumOd

trajanje <= 6h   →  satnaTarifa × sati
trajanje <= 24h  →  dnevnaTarifa × 1
inače:
    puniDani = floor(trajanje / 24h)
    ostatak  = trajanje − (puniDani × 24h)
    GRACE    = 59 minuta
    dani     = ostatak > GRACE ? puniDani + 1 : puniDani

popust: 3–6 dana → 5 %,  7+ dana → 10 %
sezonski množilac iz tabele Cjenovnik primjenjuje se na osnovicu

ukupno = (osnovica × mnozilac − popust)
       + Σ(oprema.CijenaPoDanu × kolicina × dani)
       + (osiguranje.CijenaPoDanu × dani)
       + vozilo.IznosDepozita
```

Onih 59 minuta su tu da najam koji kasni pola sata ne postane skuplji za cijeli dan.
Prekoračenje veće od toga se naplaćuje kao puni dan.

Kontrolni primjeri koje pokrivaju unit testovi:

| Trajanje | Rezultat |
|---|---|
| 5 h | satna tarifa |
| 24 h 30 min | 1 dan |
| 25 h | 2 dana |
| 48 h | 2 dana |
| 49 h | 3 dana |
| 7 dana | 7 dana uz popust 10 % |

---

## Dostupnost

> ⬜ Faza 9. Jedino mjesto gdje se odlučuje je li vozilo slobodno je
> `AvailabilityService`, i poziva se i iz pretrage i iz kreiranja rezervacije.

```
BUFFER = 2 sata

zauzeto  ⟺  trazeniOd < postojeciDo + BUFFER
         ∧  trazeniDo + BUFFER > postojeciOd
```

Uslov djeluje neintuitivno dok se ne pročita naglas: dva perioda se **ne** preklapaju
jedino ako jedan cijeli završi prije nego drugi počne. Sve ostalo je preklapanje.
Buffer od dva sata je vrijeme za pripremu vozila između dva najma.

Zauzećem se smatra rezervacija u statusu `Confirmed`, rezervacija u statusu `Pending`
čiji `DrziDo` još nije istekao, i svaka `BlokadaVozila` koja se preklapa.

Provjera ide kroz `Where` uslov **na bazi**, unutar transakcije. Učitavanje svih
rezervacija u memoriju pa filtriranje LINQ-om uputstvo izričito navodi kao grešku.

---

## Kategorije vozačkih dozvola

> ⬜ Faza 10.

| Kategorija | Šta pokriva |
|---|---|
| A1 | skuteri i motocikli do 125 cm³ |
| A | svi motocikli i skuteri, bez ograničenja — hijerarhijski uključuje A1 |
| B | quadovi |

Ta pravila su **podatak u tabeli `PravilaKategorije`**, sa poljima `MaxKubikaza`,
`MaxSnagaKw` i `MinGodine` — ne `if` grane u kodu. Ako se propis promijeni, ispravka
je unos podatka, a ne rekompajliranje.

`DozvolaService.DozvoljeneKategorijeAsync(userId)` je jedina implementacija te logike
i poziva se na tri mjesta: pretraga vozila, preporuke, i provjera preduslova pri
kreiranju rezervacije. Filtriranje ide na serveru — ne skrivanjem stavki u interfejsu,
jer se to zaobiđe direktnim pozivom API-ja.

---

## Povrat novca

| Kad se otkazuje | Povrat |
|---|---|
| više od 7 dana prije preuzimanja | 100 % |
| 3–7 dana prije | 50 % |
| manje od 48 h prije | 0 % |
| agencija otkazuje | 100 %, uvijek |

Depozit se vraća **uvijek**, neovisno o politici.

Iznos se računa iz `Placanje.NaplaceniIznos` — stvarno naplaćenog iznosa koji je
vratio Stripe — a nikad ponovnim obračunom iz cjenovnika. Razlika je bitna: kad se
tarifa promijeni u međuvremenu, povrat za staru rezervaciju mora ostati isti.

`GET /api/rezervacije/{id}/obracun-otkazivanja` vraća izračunati iznos **bez
izvršavanja akcije**, da klijent vidi tačan iznos prije nego potvrdi.

---

## Sistem preporuke

> ⬜ Faza 16. Detaljan opis modela ide u `recommender-dokumentacija.md`.

```
skor = 0,6 × content + 0,4 × popularity
```

Content komponenta je ponderisano poklapanje profila korisnika sa atributima vozila:
tip vozila 0,35 · cjenovni rang 0,25 · grad ili poslovnica 0,20 · marka 0,10 ·
kubikažni razred 0,10. Profil se gradi iz `HistorijaPretrage` i završenih rezervacija.

Popularity komponenta je normalizovan broj rezervacija u zadnjih 90 dana i Bayesova
prosječna ocjena. Novi korisnik bez historije dobija čistu popularity listu.

Rezultat se na kraju filtrira na vozila slobodna u terminu i dozvoljena za korisnikove
kategorije — isti filter kao u pretrazi.

> Uputstvo (sekcija 2.4) izričito kaže da svaki signal naveden u dokumentaciji mora
> biti stvarno korišten u kodu, i navodi prosječnu ocjenu kao primjer signala koji se
> spomene pa ignoriše. Ako se algoritam ikad pojednostavi, dokumentacija se mijenja u
> istom commitu.

---

## Sigurnost

| Pravilo | |
|---|---|
| `[Authorize]` na svim kontrolerima, `[AllowAnonymous]` samo na `login` i `register` | ✅ |
| `userId` uvijek iz JWT tokena kroz `IHttpContextAccessor` | ✅ |
| `RegisterRequest` bez polja `Role` i `IsAdmin` | ✅ |
| Odjava invalidira token na serveru — `OpozvaniToken` plus middleware | ✅ |
| Upload i download provjeravaju vlasništvo nad resursom | ⬜ |
| MIME tip se validira po magic bytes, ne po ekstenziji | ⬜ |
| Lozinke kroz BCrypt | ✅ |
| Kodovi i tokeni kroz `RandomNumberGenerator`, nikad `System.Random` | ⬜ |
| Sve tajne u `.env`, ništa osjetljivo u `appsettings.json` | ✅ |
| Docker tagovi eksplicitno verzionisani | ✅ |

---

## Kredencijali

Sve seed lozinke su `test`.

| Kontekst | Korisničko ime |
|---|---|
| Desktop | `desktop` |
| Mobilna | `mobile` |
| Administrator | `administrator` |
| Uposlenik | `uposlenik` |

Sve četiri prijave su provjerene nakon faze 6 i rade. To nije formalnost: seed i
prijava moraju koristiti **isti BCrypt format**, jer ako se raziđu, nijedan seed
korisnik se ne može prijaviti — a prijava je prva stvar koju profesor proba. Ovdje se
poklapaju po konstrukciji: seeder zove `BCrypt.HashPassword("test")`, prijava
`BCrypt.Verify`, ista biblioteka i isti podrazumijevani parametri.

Ovo je i razlog zašto je seed napisan kao runtime seeder, a ne kroz EF-ov `HasData`.
U `HasData` hash bi morao biti statička konstanta zalijepljena u kod, jer poziv
`HashPassword` daje drugačiji rezultat pri svakom pokretanju — EF bi pri svakoj
migraciji vidio promjenu i generisao novu migraciju bez ijedne stvarne izmjene modela.
Isto vrijedi za `DateTime.UtcNow`. Runtime seeder tih ograničenja nema: hashira pri
pokretanju, a determinističnost datuma i nasumičnih vrijednosti postiže fiksnim
`Random(220182)` sjemenom.

---

## Komande

```bash
# cijeli stack
docker compose up --build

# samo baza i broker, za razvoj sa hosta
docker compose up -d sunnyrides-db sunnyrides-rabbitmq

# migracije
dotnet ef migrations add NazivMigracije --project SunnyRides.Services --startup-project SunnyRides.API
dotnet ef database update --project SunnyRides.Services --startup-project SunnyRides.API

# brza provjera baze
docker exec sunnyrides-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "<lozinka>" \
  -Q "USE [220182]; SELECT COUNT(*) FROM sys.tables;"
```
