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
| 9 | Obračun cijene i provjera dostupnosti | ✅ |
| 10 | Vozačke dozvole i filtriranje po kategoriji | ✅ |
| 11 | Rezervacije, state machine, otkazivanje | ✅ |
| 12 | Plaćanje, webhook, povrat novca | ✅ |
| 12a | Razlozi otkazivanja kao šifrarnik | ✅ |
| 13 | Primopredaja i obračun depozita | ✅ |
| 14 | RabbitMQ i worker servis | ✅ |
| 15 | Notifikacije i SignalR | ✅ |
| 16 | Sistem preporuke | ✅ |
| 17–18 | Desktop i mobilna aplikacija | ⬜ |
| 19 | PDF izvještaji | ✅ |

### Šta prijava obećava, a plan izrade nema kao zasebnu fazu

Prijava je ugovor — uputstvo (2.1) kaže da sve navedeno u njoj mora biti implementirano
i da implementacija mora odgovarati opisu. Poređenjem prijave sa planom izrade i sa
kodom izdvojilo se ovo što još nema svoje mjesto u fazama 13–19, pa se ovdje vodi da
ne ispadne:

| Iz prijave | Šta treba na backendu | |
|---|---|---|
| Upravljanje korisnicima i ulogama, administratorski reset lozinke | CRUD korisnika za administratora, dodjela uloga, reset bez stare lozinke | ✅ |
| Pregled i izmjena profila, profilna fotografija | izmjena vlastitih podataka i upload slike (magic bytes, vlasništvo) | ✅ |
| Reset lozinke kodom poslanim na email | kod kroz `RandomNumberGenerator`, čuvan kao hash, sa rokom (`KodZaResetLozinke`) | ✅ faza 14b |
| Moderacija recenzija, ocjenjivanje nakon `Completed` | CRUD recenzija, skrivanje umjesto brisanja | ✅ |
| Obavijesti agencije na početnom ekranu | CRUD obavijesti sa slikom | ✅ |
| Pregled poslovanja (četiri kartice, raspored za danas, iskorištenost) | jedan agregatni endpoint, `GroupBy` na bazi | ✅ |
| Kalendar flote i ručni unos rezervacije klikom na slobodan raspon | endpoint za sedmicu po vozilima; kreiranje rezervacije od strane osoblja za navedenog klijenta | ✅ |
| Blokada: zamjena vozila za pogođenu rezervaciju | prebacivanje rezervacije na vozilo istog ili boljeg ranga, uz ponovnu provjeru dostupnosti | ⬜ |
| Pretraga sa ukupnom cijenom za cijeli period i sortiranjem po cijeni, ocjeni i preporuci | cijena po vozilu u rezultatu pretrage, prosječna ocjena | ⬜ |
| Historija pretrage kao ulaz za preporuke | upis u `HistorijaPretrage` pri svakoj pretrazi koja nosi filter, iz `VoziloService.GetAsync` | ✅ faza 16 |
| Detalji vozila: recenzije i slična vozila | slična vozila su gotova (`/api/preporuke/slicna/{id}`); lista recenzija po vozilu još nije | 🟡 |
| Otkazivanje: „korisnik bira razlog iz padajuće liste" | šifrarnik `RazlogOtkazivanja`, padajuća lista se puni iz baze | ✅ |

Dvije stvari iz prijave su riješene malo drugačije nego što tekst doslovno kaže, i to
je namjerno:

- Prijava kaže da klijent dozvolu prijavljuje „pri registraciji". Dozvola se prijavljuje
  odmah poslije registracije, zasebnim zahtjevom, jer traži fotografiju, a upload
  fotografije traži prijavljenog korisnika radi provjere vlasništva. Registracija je
  `[AllowAnonymous]` i zato ostaje bez fajlova. Za korisnika je to isti tok.
- Politika otkazivanja u specifikaciji je imala rupu između 48 sati i 3 dana. Pravilo je
  zatvoreno kao „manje od 3 dana → 0 %" (vidi sekciju o povratu novca).

### Ostaci koje treba počistiti prije predaje

- `SunnyRides.API/SunnyRides.API.http` je ostatak šablona i poziva `/weatherforecast`
  (uputstvo 8.1 to navodi kao primjer za odbijanje).
- `SunnyRides.Subscriber/Worker.cs` je još šablonski worker koji samo loguje; zamjenjuje
  ga faza 14 (uputstvo 3.2: pomoćni servis mora raditi stvarne zadatke).

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
traži svjestan potez. `[AllowAnonymous]` u cijelom projektu postoji na četiri mjesta,
sva četiri u `AuthController`-u: `login`, `register` i dva koraka reseta zaboravljene
lozinke. Zadnja dva su dodata u fazi 14b i o njima piše sekcija „Reset zaboravljene
lozinke", zajedno sa otvorenim pitanjem za mentora.

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

Atributi stoje na `SifrarnikController`, a ne na svakom kontroleru posebno. Razlog je
isti kao kod `[Authorize]` na `BaseController` — ono što se piše na svakom mjestu, na
nekom se zaboravi. To se pokazalo kad je naknadno dodan šifrarnik razloga otkazivanja:
novi kontroler je dobio istu zaštitu bez ijedne linije koda za nju.

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

## Provjera dostupnosti

Ovo je drugi od dva problema koji čine domen specifičnim: isto vozilo ne smije biti
izdato dva puta u periodima koji se preklapaju. Odgovor na pitanje „je li vozilo
slobodno" daje se na **jednom** mjestu, a traže ga tri: pretraga vozila, kreiranje
rezervacije i kalendar flote.

Dvije implementacije istog uslova znače da pretraga pokaže vozilo koje rezervacija
odbije — ili, gore, da rezervacija prihvati termin koji je već zauzet.

### Uslov preklapanja

```
BUFFER = 2 sata

zauzeto  ⟺  postojeciDo > (traženiOd − BUFFER)
         ∧  postojeciOd < (traženiDo + BUFFER)
```

Buffer postoji zato što vozilo između dva najma treba oprati, dopuniti gorivo i
pregledati. Bez njega bi sistem dozvolio da jedan klijent vrati skuter u 10:00, a
drugi ga preuzme istog trenutka.

Oduzimanje i dodavanje buffera dešava se na jednom mjestu — `UslovDostupnosti.GranicaOd`
i `GranicaDo`. Svaki upit gradi granice kroz njih. Da se aritmetika piše svugdje gdje
se sastavlja upit, prvi propušteni buffer bio bi dvostruko ugovoren termin.

Uslov je **strogo** preklapanje, pa termin koji počinje tačno dva sata nakon kraja
prethodnog prolazi. To je i potvrđeno testom:

| Novi termin | Slobodno |
|---|---|
| isti kao postojeća rezervacija | ne |
| počinje 30 min nakon kraja | ne |
| počinje 1 h nakon kraja | ne |
| počinje **tačno 2 h** nakon kraja | da |
| završava 1 h prije početka | ne |
| završava **tačno 2 h** prije početka | da |
| obuhvata rezervaciju sa obje strane | ne |

Granica je simetrična — buffer se dodaje sa obje strane traženog perioda, ne samo
poslije.

### Šta se smatra zauzećem

| Zapis | Uslov | Buffer |
|---|---|---|
| Rezervacija `Confirmed` | uvijek | da |
| Rezervacija `Pending` | samo dok `DrziDo > sada` | da |
| `BlokadaVozila` | uvijek | **ne** |

`Cancelled` ne zauzima ništa, a `Completed` je prošlost. `Pending` zauzima samo dok
traje držanje termina — kad istekne, vozilo je slobodno i prije nego ga periodični
posao formalno otkaže. Da nije tako, neplaćena rezervacija držala bi termin do
sljedećeg prolaska workera.

**Blokade se gledaju bez buffera**, i to je svjesna odluka. Buffer je vrijeme za
pripremu vozila između dva *najma*; servis nije najam. Specifikacija to i nagovještava
formulacijom — za rezervacije kaže „rezervacija u statusu X", a za blokade „svaka
`BlokadaVozila` **koja se preklapa**".

### Zašto uslov vraća `IQueryable`, a ne listu

`DodajUslovSlobodno` prima upit nad vozilima i vraća isti upit sa dodatim uslovom:

```csharp
upit.Where(v => !v.Rezervacije.Any(...)).Where(v => !v.Blokade.Any(...))
```

Rezultat je **jedan** SQL upit sa dva `NOT EXISTS`. Alternativa — dohvatiti
identifikatore slobodnih vozila pa ih proslijediti kroz `Contains` — značila bi dva
upita i listu koja raste sa veličinom flote. Uputstvo učitavanje svega u memoriju pa
naknadno filtriranje izričito navodi kao grešku.

Zato `VoziloService` ne piše svoj uslov nego traži od `AvailabilityService` da ga
doda. Pretraga i rezervacija time dijele isti kod, a ne isto pravilo napisano dvaput.

### Zaključavanje vozila

`ZakljucajVoziloAsync` je jedini raw SQL u projektu:

```sql
SELECT TOP 1 Id FROM Vozilo WITH (UPDLOCK, HOLDLOCK) WHERE Id = @id
```

EF nema način da izrazi lock hint, a bez njega dvije istovremene rezervacije mogu obje
proći provjeru dostupnosti prije nego ijedna upiše svoj red. `UPDLOCK` uzima lock koji
drugi čitač sa istim hintom mora čekati; `HOLDLOCK` ga drži do kraja transakcije
umjesto do kraja upita.

Metoda baca izuzetak ako je neko pozove izvan transakcije — lock bez transakcije se
otpušta odmah po izvršenju upita i ne štiti ništa. To je greška u kodu koji poziva, a
ne stanje koje korisnik može izazvati, pa pada glasno.

Zaštita radi pod uslovom da **svi** koji upisuju rezervacije uzmu isti lock. Pošto je
`RezervacijaService` jedini pisac, to je ispunjeno. Drugi sloj je jedinstveni indeks
na `(KorisnikId, VoziloId, DatumOd)`, ali on hvata samo dvostruko slanje iste forme
od istog korisnika.

> ✅ **Test konkurentnosti je proveden u fazi 11**, kad je nastao endpoint za
> kreiranje rezervacije. Dva istovremena zahtjeva za isto vozilo i isti termin: jedan
> prolazi, drugi dobija 400, a u bazi ostaje tačno jedna rezervacija. Detalji su u
> sekciji o rezervacijama.

### Pogođene rezervacije

`GET /api/dostupnost/pogodjene-rezervacije` vraća potvrđene rezervacije koje bi
planirana blokada pogodila, sa imenom, emailom i telefonom klijenta — da uposlenik
zna koga treba nazvati prije nego blokadu potvrdi. Ovo je stavka koja je u fazi 8
ostala nenapravljena, upravo zato što uslov preklapanja pripada ovdje.

Endpoint je isključivo za osoblje. Klijent nema razloga znati ko još ima rezervaciju
na tom vozilu, a obična provjera dostupnosti mu je otvorena.

---

## Ispravka u seed podacima

Test dostupnosti otkrio je grešku u seederu koja bi se inače pojavila tek u fazi 17.

U bazi nije bilo **nijedne** rezervacije u statusu `Confirmed` ni `Pending`, i nijedne
u budućnosti — samo 15 otkazanih i 80 završenih, sve starije od tri mjeseca.

Uzrok je bio u generatoru. Petlja je po vozilu pravila **fiksan broj** rezervacija —
dvije do četiri — a svaka pomjeri kursor za prosječno devetnaest dana. Prozor je 250
dana (190 unazad, 60 unaprijed), pa je zadnja rezervacija po vozilu padala oko 114
dana prije današnjeg dana. Horizont se nikad nije ni približio.

Status se izvodi iz odnosa termina prema danas: `datumDo < danas` daje `Completed` ili
`Cancelled`, `datumOd > danas` daje `Confirmed` ili `Pending`. Pošto budućih termina
nije bilo, druga grana se nikad nije izvršila.

Popravka je da petlja ide **dok ne dođe do horizonta**, sa gornjom granicom po vozilu
samo da jedno vozilo ne popuni cijeli kalendar.

Šta bi ovo oborilo da nije nađeno: kalendar flote bio bi prazan, kartica „aktivne
rezervacije" na pregledu poslovanja pokazivala bi nulu, `popularity` komponenta
preporuke broji rezervacije iz zadnjih 90 dana — a njih nije bilo nijedne — i state
machine iz faze 11 ne bi imao nijedan `Pending` ni `Confirmed` zapis. Uputstvo uz to
traži rezervacije **u svim statusima**.

Nakon ispravke: 15 `Pending`, 67 `Confirmed`, 76 `Cancelled`, 205 `Completed`, sa
terminima do šezdeset dana u budućnost.

> Seeder se pokreće samo nad praznom bazom, pa je za primjenu bio potreban
> `docker compose down -v`. To je ista komanda kojom se u fazi 20.6 provjerava da se
> sistem podiže iz ničega.

---

## Vozačke dozvole i kategorije

Ovo je drugi problem koji čini domen specifičnim: klijentu se ne smije ponuditi
vozilo koje prema svojoj dozvoli ne smije voziti.

### Hijerarhija nije napisana u kodu

Nigdje u projektu ne stoji da „A pokriva A1". To se **izvodi iz tabele**
`PravilaKategorije`:

| Kategorija | Tip vozila | Max kubikaža | Max snaga | Min godine |
|---|---|---|---|---|
| A1 | skuter | 125 cm³ | 11 kW | 16 |
| A1 | motocikl | 125 cm³ | 11 kW | 16 |
| A | skuter | — | — | 24 |
| A | motocikl | — | — | 24 |
| B | quad | — | — | 18 |

Pravilo kategorije A za motocikle nema gornju granicu, a pravilo A1 ima 125 cm³ i
11 kW. Ko ima A, ima pravilo koje pokriva sve što pokriva i A1 — pa je hijerarhija
**posljedica podataka**, ne grana u kodu.

`PravilaPokrivenosti.Pokrivene` radi po jednom pitanju: pokriva li neka moja kategorija
sve što traži kandidat? Pokriva ako za svaki tip vozila koji kandidat obuhvata imam
vlastito pravilo koje nije strožije. Prazna granica znači „bez ograničenja" i pokriva
svaku konkretnu; obrnuto ne vrijedi, jer bi inače A1 ispao ravnopravan sa A.

Ako se propis promijeni i A dobije granicu od 500 cm³, A i dalje pokriva A1 — 500 je
veće od 125. Ispravka je izmjena jednog reda u šifrarniku; kod se ne dira. To je i
testirano: jedan test mijenja pravila i provjerava da se hijerarhija promijenila sama.

**Godine ulaze u račun**, jer pravilo nosi i `MinGodine`. Neko ko ima upisanu
kategoriju A a ima dvadeset godina ne dobija njena prava — pravilo traži 24, pa mu ta
kategorija ne vrijedi ni za A1.

### Rok se provjerava na datum preuzimanja

Dozvola koja važi danas a ističe prije termina ne pokriva taj najam. Zato
`DozvoljeneKategorijeAsync` prima datum, a pretraga mu prosljeđuje početak traženog
termina. Provjera na današnji dan propustila bi rezervaciju za termin nakon isteka.

### Šta zahtjev namjerno nema

`VozackaDozvolaRequest` nema polje `KorisnikId` — vlasnik se čita iz tokena. Nema ni
`Status`: da ga ima, klijent bi sam sebi odobrio dozvolu i preskočio verifikaciju.

`VoziloSearchObject.SamoDozvoljenaZaMene` je **zastavica, a ne lista kategorija**. Da
klijent šalje kategorije, poslao bi one koje mu odgovaraju; ovako ih server izvodi iz
njegove dozvole. Razriješene kategorije žive u privatnom polju servisa, a ne u search
objektu — search objekat se puni iz query stringa, pa bi lista u njemu bila nešto što
klijent može sam poslati.

### Dvije strane istog kontrolera

Klijent radi isključivo sa **svojom** dozvolom i nikad ne navodi čijom — rute za njega
nemaju identifikator u putanji (`/api/dozvole/moja`, `/api/dozvole/moje-kategorije`).
Da ga imaju, bilo bi dovoljno promijeniti broj u URL-u da se vidi tuđa dozvola, i
nijedan `[Authorize]` to ne bi spriječio.

Uposlenik radi po identifikatoru, jer verifikuje tuđe dozvole, i te rute traže ulogu.

### Verifikacija

Svaka izmjena dozvole vraća status na `NaCekanju` i briše tragove ranije odluke. Bez
toga bi klijent mogao dobiti odobrenje, pa poslije promijeniti broj i kategorije — a
odobrenje bi ostalo.

Razlog je obavezan **samo pri odbijanju**. Odobrenje i odbijanje se mogu ispraviti u
oba smjera (uposlenik pogriješi), ali se ponavljanje iste odluke odbija sa porukom
„Dozvola je već odobrena" — nema smisla dvaput odobriti istu stvar.

Istekla dozvola se ne može odobriti: klijent mora prijaviti važeću.

**Uposlenik na ekranu za verifikaciju vidi šta bi klijent smio voziti**, izvedeno iz
kategorija i pravila, i to **bez obzira na status** — on to čita dok odlučuje hoće li
odobriti, pa mu odgovor „čeka verifikaciju" ne bi pomogao. Oba puta, klijentski i
uposlenički, računaju pokrivenost kroz istu metodu, pa ne mogu dati različit odgovor
za istu dozvolu.

### Jedan oblik greške za cijeli API

Greške iz anotacija i greške iz servisa sada izgledaju isto.

`[ApiController]` po defaultu vraća `ValidationProblemDetails` sa rječnikom `errors`,
dok `ExceptionFilter` vraća `ProblemDetails` sa poljem `detail`. To su dva oblika istog
događaja — zahtjev nije prihvaćen — i klijentska aplikacija bi za svaki morala imati
vlastito čitanje.

`InvalidModelStateResponseFactory` sada spaja poruke iz anotacija u isti
`ProblemDetails`. Uputstvo traži da se backend validacijska poruka proslijedi
korisniku umjesto generičkog teksta, a to je lakše ispuniti kad postoji samo jedan
oblik odgovora.

### Testovi kojima je korak zatvoren

Devet novih unit testova pokriva hijerarhiju: A pokriva A1, A1 ne pokriva A, B je
odvojena, više kategorija se sabira, mlađi od propisane dobi ne dobija prava, tačno
propisana dob ih daje, i — najvažnije — promjena pravila mijenja hijerarhiju bez
ijedne izmjene koda.

Kroz API je provjereno ono što unit testovi ne mogu:

| Test | Rezultat |
|---|---|
| Klijent sa A i B | smije A, A1, B — svih 33 vozila |
| Klijent sa samo A1 | smije samo A1 — 20 od 33 vozila |
| Uposlenik gleda dozvolu na čekanju | „Sa kategorijama A1 i 35 godina, klijent bi smio voziti vozila kategorija A1." |
| Odbijanje bez razloga | 400 sa porukom iz anotacije |
| Dvostruko odobrenje | 400, „Dozvola je već odobrena" |
| Klijent traži listu svih dozvola | 403 |

---

## Privatni fajlovi

Fotografija vozačke dozvole je prvi osjetljivi fajl u sistemu. Tu se sastaju tri
pravila iz uputstva koja su dotad bila samo pripremljena: odvojeni korijen, validacija
po magic bytes i provjera vlasništva nad resursom.

### Ključ, ne adresa

`SacuvajPrivatnoAsync` vraća **ključ** — `dozvole/12/a1b2.jpg` — bez prefiksa i bez
vodeće kose crte. Ono što završi u bazi namjerno ne liči na adresu, jer se do
privatnog fajla dolazi isključivo kroz endpoint koji provjerava vlasništvo.

Javne i privatne slike imaju **odvojene metode**, a ne jednu sa zastavicom. Tako se ne
može desiti da neko previdi parametar i osjetljivu fotografiju snimi u folder koji se
poslužuje statički. Isto vrijedi za brisanje.

Privatna slika nema thumbnail. Mala verzija osjetljivog dokumenta je i dalje osjetljiv
dokument, a nigdje se ne prikazuje u listi.

### Vlasništvo se provjerava u servisu, prema tokenu

```csharp
var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();
var jeOsoblje = _trenutniKorisnik.JeUUlozi(Uloge.Administrator)
                || _trenutniKorisnik.JeUUlozi(Uloge.Uposlenik);

if (dozvola.KorisnikId != korisnikId && !jeOsoblje)
{
    throw new ForbiddenException("Mozete preuzeti samo fotografiju svoje dozvole.");
}
```

`[Authorize]` ovdje ne bi uhvatio ništa — i klijent i uposlenik su uredno prijavljeni
korisnici. Razlika je u tome čija je dozvola, a to zna samo servis.

Rute su podijeljene po istom principu kao i ostatak modula. **Upload** ide na
`/api/dozvole/moja/fotografija`, bez identifikatora — dozvola se pronalazi po
korisniku iz tokena, pa se tuđa ne može ni adresirati. **Preuzimanje** ima
identifikator, jer uposlenik mora moći otvoriti tuđu, i tu provjera radi posao.

### Dvije strane, ne jedna

Dozvola se fotografiše s obje strane: na prednjoj su ime, broj i rok važenja, na
zadnjoj **kategorije** i datumi po kategoriji. Jedna fotografija nije dovoljna da se
dozvola provjeri — a kategorije su upravo ono zbog čega se dozvola i verifikuje, jer
određuju šta klijent smije voziti.

Zato entitet ima dvije putanje (`PutanjaSlikePrednja`, `PutanjaSlikeZadnja`), otprema
prima `strana` uz fajl, a preuzimanje uz identifikator. Odobrenje **odbija** dozvolu
kojoj neka strana nedostaje; desktop to i ne dozvoljava — dugme „Odobri" je sivo dok
obje nisu tu, pa uposlenik vidi da mu nedostaje podatak prije klika, a ne poslije.

Migracija postojeću kolonu preimenuje u prednju stranu (to je ono što se dosad tražilo
od klijenta) i zadnju postavlja na isti fajl, da već odobrene dozvole ne postanu
nepotpune preko noći. Zbog toga dvije putanje mogu pokazivati na isti fajl, pa servis
pri zamjeni jedne strane provjerava pokazuje li na taj fajl i druga — inače bi zamjena
prednje strane obrisala sliku zadnje.

### Nova fotografija vraća dozvolu na čekanje

Bez toga bi klijent odobrenu dozvolu mogao zamijeniti drugom slikom, a odobrenje bi
ostalo da važi. Isto pravilo kao kod izmjene broja i kategorija.

Redoslijed pri zamjeni je isti obrazac kao kod slika vozila: nova se snima, pa se
upisuje u bazu, pa se tek onda briše stara. Ako upis padne, nova se briše u `catch`
bloku — inače bi ostala bez ijednog zapisa koji na nju pokazuje.

### Provjera izlaska iz foldera

`UApsolutnu` poredi punu putanju sa korijenom koji **završava separatorom**. Bez toga
bi folder `privatno-backup` prošao provjeru da je „unutar" foldera `privatno`, jer
`"...\privatno-backup\x.jpg".StartsWith("...\privatno")` vraća tačno.

Kod javnih slika to bi bilo nezgodno; kod privatnih znači čitanje fajlova koje niko ne
bi smio vidjeti.

### Gdje folderi zapravo jesu

Testom se pokazalo da je `privatno/` nastao u `SunnyRides.API\privatno`, a ne u
korijenu repozitorija.

Uzrok: svaki folder se tražio zasebno. `uploads` postoji u repozitoriju pa se našao
jedan nivo iznad radnog foldera; `privatno` ne postoji nigdje, pa je pretraga pala na
radni folder — a to je `SunnyRides.API`.

Sigurnost time nije bila ugrožena: folder se i dalje nije posluživao statički, a
`.gitignore` ga hvata na bilo kojem nivou. U kontejneru bi bilo gore — Compose montira
samo `./uploads`, pa bi `/app/privatno` živio unutar image-a i **nestao pri svakom
`--build`**.

Popravka je da se traži **jedan korijen podataka** — folder koji sadrži `uploads` — a
oba se izvedu kao susjedi ispod njega. Traži se samo javni jer on postoji u
repozitoriju; privatni se pravi pri prvom pokretanju, pa ga nema smisla tražiti.

Uz to je dodato `./privatno:/app/privatno` u `docker-compose.yml` i `privatno/` u
`.dockerignore` — fajlovi se montiraju, ne pakuju u image.

### Placeholder u seedu

Seed nema stvarne skenove vozačkih dozvola i ne bi ih smio ni imati. Ali bez ijedne
fotografije ekran za verifikaciju nema šta prikazati, pa se pri seedu generiše jedna
neutralna slika koju dijele sve seed dozvole.

Ide kroz **istu pohranu** kao stvarni upload, dakle u privatni folder — da se ni u
seedu ne uvede izuzetak od pravila da osjetljivi fajlovi nisu javni.

### Testovi kojima je faza zatvorena

| Test | Očekivano | Dobiveno |
|---|---|---|
| Vlasnik preuzima svoju fotografiju | 200 | ✅ |
| Drugi klijent traži istu | **403** | ✅ |
| Uposlenik traži tuđu | 200 | ✅ |
| Privatni folder kroz `/uploads/...` | 404 | ✅ |
| Privatni folder kroz `/privatno/...` | 404 | ✅ |
| Upload vraća dozvolu na čekanje | status `NaCekanju` | ✅ |
| Tekstualni fajl sa `.jpg` ekstenzijom | 400, ništa na disku | ✅ |
| `git status` vidi `privatno/` | ne vidi | ✅ |
| DTO sadrži putanju do fotografije | ne sadrži, samo `imaFotografiju` | ✅ |

---

## Rezervacije i promjena statusa

### Tabela prelaza je odvojena od mašine

`PrelaziRezervacije` nema nijednu zavisnost — ni bazu, ni korisnika, ni audit. Cijelo
pravilo stoji na jednom mjestu koje se pročita odjednom:

```
Pending   → Confirmed    plaćanje verifikovano na serveru
Pending   → Cancelled    isteklo držanje ili klijent odustao
Confirmed → Cancelled    otkazivanje prije preuzimanja
Confirmed → Completed    evidentiran povrat vozila
Cancelled → terminalno
Completed → terminalno
```

**Sve što u toj tabeli ne piše je zabranjeno.** Nema implicitnih prelaza i nema
prelaza koji se „podrazumijeva" — uključujući prelaz u isti status, koji testovi
posebno gađaju.

Poruka o odbijanju nabraja šta jeste moguće: „Rezervacija je na čekanju i ne može
postati završena. Iz ovog stanja moguće je samo: potvrđena, otkazana." Korisnik time
zna šta da uradi umjesto da pogađa.

Jedan test postoji samo da zaključa odluku koja se lako previdi: **izdavanje vozila ne
mijenja status.** Rezervacija ostaje `Confirmed` i dok je vozilo fizički kod klijenta —
fizički tok se prati kroz zapise `Primopredaja`, jer uputstvo propisuje tačno četiri
statusa. Da je neko dodao `InProgress`, rad bi pao na tom pravilu.

### Mašina ne snima

`Promijeni` i `ZabiljeziKreiranje` su **sinhrone** i namjerno ne zovu
`SaveChangesAsync`. Promjena statusa je uvijek dio veće operacije — naplate,
otkazivanja, povrata vozila — pa transakcijom upravlja onaj ko tu operaciju vodi. Da
mašina sama snima, kreiranje rezervacije bi imalo dva odvojena upisa umjesto jednog
atomskog.

Audit zapis nosi četiri stvari koje uputstvo traži: ko, kada, razlog i opis. Izvršilac
se čita iz tokena i **smije biti prazan** — periodični posao u workeru otkazuje
istekle rezervacije bez ijednog prijavljenog korisnika, i tada je tačno reći da to
nije uradio niko nego sistem.

### Redoslijed pri kreiranju nije proizvoljan

```
1. provjera klijenta (blokiran? deaktiviran?)
2. provjera termina (nije u prošlosti, kraj poslije početka)
   ── otvara se transakcija ──
3. ZAKLJUČAVANJE VOZILA
4. učitavanje vozila, provjera da je aktivno
5. provjera dozvole na datum preuzimanja
6. provjera dostupnosti
7. provjera zaliha opreme
8. obračun cijene na serveru
9. upis rezervacije i stavki
   ── commit ──
```

**Zaključavanje ide prije provjere dostupnosti.** Da je obrnuto, dva istovremena
zahtjeva bi oba prošla provjeru prije nego ijedan upiše svoj red — provjera bi bila
tačna u trenutku kad je rađena i pogrešna čim se drugi zahtjev upiše.

**Provjera dozvole se radi ponovo**, iako je ista provjera već filtrirala pretragu.
Filter u pretrazi je udobnost; provjera pri kreiranju je pravilo. Bez druge, klijent
bi zaobišao filter pozivom API-ja direktno.

### Broj se izvodi iz identifikatora, pa se upisuje u dva koraka

`Broj` ima jedinstveni indeks, a identifikator baza dodjeljuje tek pri upisu. Zato
prvi upis nosi privremenu vrijednost sa GUID-om, a drugi konačan broj
`SR-2026-01002`. Oba su u istoj transakciji, pa privremenu vrijednost niko izvana ne
vidi.

Alternativa — brojanje postojećih rezervacija za tu godinu — kod dva istovremena upisa
dala bi isti broj i pala na jedinstvenom indeksu.

### Šta zahtjev nema

`RezervacijaInsertRequest` nema nijedno polje koje nosi novac ni stanje: nema
`UkupanIznos`, `IznosDepozita`, `IznosPopusta`, `Status`, `IsPaid` ni `DrziDo`. Sve to
računa i postavlja server.

Nema ni `KorisnikId` — klijent rezerviše za sebe. Nema ni `PoslovnicaId`, jer se vozilo
preuzima tamo gdje jeste.

Kreiranje je otvoreno **samo klijentu**. Ručni unos rezervacije od strane osoblja, iz
kalendara flote, traži da se klijent navede izvana; to je zaseban endpoint sa vlastitom
provjerom uloge i dolazi uz kalendar u fazi 17. Ovaj put se time ne otvara.

### Blokiran klijent dobija 403, ne 400

Blokada ne oduzima pristup vlastitim podacima nego pravo na novi najam. Blokiran
klijent se može prijaviti, vidjeti svoju historiju i račune, ali ne može rezervisati.

### Čitanje je ograničeno vlasništvom u servisu

Klijent vidi isključivo svoje rezervacije, osoblje sve. Ograničenje se **nalaže u
servisu**, a ne očekuje od klijenta da pošalje ispravan filter — razriješeni
identifikator živi u privatnom polju servisa, jer se search objekat puni iz query
stringa.

Dohvat po identifikatoru provjerava vlasništvo posebno: bez toga bi svaki prijavljen
klijent mogao mijenjati broj u adresi i čitati tuđe rezervacije, zajedno sa iznosima i
kontakt podacima.

### Lista i detalj vuku različito

Detaljni dohvat učitava tri kolekcije — slike vozila, stavke opreme i historiju
statusa — pa ide kroz `AsSplitQuery`. U jednom upitu bi se redovi množili međusobno:
rezervacija sa tri stavke i četiri zapisa historije vratila bi dvanaest redova umjesto
sedam, i u svakom ponovo sve podatke o vozilu i klijentu. EF na to i upozorava.

Lista ima **tačno jednu** kolekciju — glavnu sliku vozila — pa se redovi ne množe i
upit ostaje jedan. Stavke opreme i historija se u listi ne prikazuju, pa se ni ne
učitavaju; na DTO-u ostaju prazne liste.

### Test konkurentnosti

Ovo je test koji je iz faze 9 ostao nenapravljen, jer je tražio endpoint koji tada nije
postojao.

Dva **različita** klijenta šalju istovremeni zahtjev za isto vozilo i isti termin.
Različita namjerno — da jedinstveni indeks na `(KorisnikId, VoziloId, DatumOd)` ne
uhvati slučaj umjesto zaključavanja, pa da lock bude taj koji odlučuje.

| | Rezultat |
|---|---|
| Prvi zahtjev | `USPJEH SR-2026-01003` |
| Drugi zahtjev | `ODBIJEN 400 — Vozilo je već rezervisano u tom terminu` |
| Rezervacija u bazi za taj termin | **1** |

`UPDLOCK, HOLDLOCK` nad redom vozila radi posao: drugi zahtjev čeka na locku, a kad
ga dobije, provjera dostupnosti već vidi upisanu rezervaciju.

### Testovi kojima je korak zatvoren

24 nova unit testa za tabelu prelaza. Kroz API:

| Test | Rezultat |
|---|---|
| Klijent vidi svoje (67), osoblje sve (363) | ✅ |
| Klijent traži tuđu rezervaciju | 403 |
| Kreiranje: `Pending`, `IsPaid=false`, držanje 899 s | ✅ |
| Iznos, depozit i popust računa server | ✅ |
| Audit zapis nastaje odmah | „Rezervacija kreirana, čeka se plaćanje." |
| Isto vozilo, isti termin, drugi klijent | 400 |
| Termin u prošlosti | 400 |
| Quad klijentu koji ima samo A1 | „Za ovo vozilo je potrebna kategorija B, a vaša dozvola pokriva A1." |
| Uposlenik kroz klijentski endpoint | 403 |
| Dvostruko slanje iste forme | 400 |

### Otkazivanje: prikaz nije obećanje

Otkazivanje ima dva endpointa. `GET /api/rezervacije/{id}/obracun-otkazivanja` kaže
šta bi se vratilo kad bi se otkazalo u ovom trenutku i ne mijenja ništa.
`POST /api/rezervacije/{id}/otkazi` otkazuje.

Obračun se pri samom otkazivanju radi **iznova**, iz podataka u bazi i iz trenutnog
vremena. Onaj prvi poziv je prikaz, a ne obećanje. Da se njegov odgovor uzimao zdravo
za gotovo, klijent bi ga mogao zatražiti osam dana prije termina — kad je povrat pun —
sačekati do dana prije, otkazati i tražiti tih sto posto. Ovo je isti princip kao kod
cijene: klijent bira šta hoće, ali koliko to košta i koliko mu se vraća računa server,
u trenutku kad se odluka stvarno izvršava.

### Pravilo povrata stoji na jednom mjestu

`PravilaOtkazivanja` je statička klasa bez baze i bez stanja, kao i `ObracunCijene`.
Razlog za odvajanje je isti: pravilo o povratu je ono što klijent citira kad se ne
slaže sa iznosom, pa mora stajati na jednom mjestu koje se može pročitati odjednom i
provjeriti bez baze.

| Kad se otkazuje | Povrat najma |
|---|---|
| više od 7 dana prije preuzimanja | 100 % |
| od 3 do 7 dana prije | 50 % |
| manje od 3 dana prije | 0 % |
| termin je već počeo | 0 % |
| agencija otkazuje | 100 %, bez obzira na rok |

Depozit se vraća **uvijek i u cijelosti**, jer nije naknada nego polog. To je i razlog
zašto je naplaćeni iznos u obračunu podijeljen na dva dijela: procenat djeluje samo na
dio najma, depozit prolazi netaknut.

Seed koristi **istu** klasu umjesto vlastite kopije pravila. Da je pravilo prepisano na
dva mjesta, prva izmjena bi napravila neslaganje između podataka u bazi i onoga što
aplikacija računa — a to je vrsta greške koja se primijeti tek kad neko uporedi stari i
novi zapis.

### Osnova je naplaćeno, ne cijena

Povrat se računa iz `Placanje.NaplaceniIznos` uspješnih plaćanja, nikad iz
`Rezervacija.UkupanIznos` i nikako ponovnim obračunom iz cjenovnika. Ako se tarifa
u međuvremenu promijenila, povrat to ne smije osjetiti — vraća se dio onoga što je
stvarno uzeto, a ne dio onoga što bi ista rezervacija koštala danas.

Iz istog razloga se prati i koliko je po rezervaciji **već** vraćeno. Bez toga bi dva
uzastopna zahtjeva — ili jedan ponovljen nakon prekida veze — napravila dva povrata za
isti novac. Kao već vraćen računa se i povrat koji je tek zapisan a nije izvršen;
neuspio i poništen se ne računaju, jer je taj novac i dalje kod agencije.

### Ko smije otkazati i šta mora navesti

Otkazuju i klijent i osoblje, pa na endpointu nema `Roles`. Razlika se ne vidi u ruti
nego u ishodu: kad otkazuje agencija, povrat je pun.

Razlog se ne upisuje slobodno, nego se bira iz liste. Prijava tako opisuje ekran za
otkazivanje, a uputstvo (sekcija 6) traži da se padajuće liste pune iz baze. Zato
razlozi žive u šifrarniku `RazlogOtkazivanja`, sa četiri oznake:

| Polje | Značenje |
|---|---|
| `ZaKlijenta` | razlog se nudi u mobilnoj aplikaciji |
| `ZaAgenciju` | razlog se nudi osoblju u desktop aplikaciji |
| `TraziNapomenu` | uz razlog se mora upisati objašnjenje (npr. „Ostalo") |
| `Aktivan` | isključen razlog se više ne nudi, ali stare rezervacije i dalje pokazuju na njega |

Server provjerava da je izabrani razlog aktivan i da je predviđen za onoga ko otkazuje.
Bez toga bi klijent kroz API mogao poslati „Vozilo je u kvaru ili na servisu" i u
historiji bi izgledalo kao da je otkazala agencija. Razlog koji je već korišten ne
može se obrisati — isključuje se.

Na rezervaciji stoje `RazlogOtkazivanjaId` i `NapomenaOtkazivanja`. Razlog je prazan
samo kad rezervaciju otkaže sistem (plaćanje stiglo nakon isteka držanja, a termin u
međuvremenu zauzet); tada je objašnjenje u napomeni. U audit zapis ide tekst razloga
zajedno sa napomenom, kakav je bio u trenutku otkazivanja.

Ko je otkazao čita se iz tokena i upisuje u `OtkazaoKorisnikId`. Tijelo zahtjeva nosi
samo razlog i napomenu. Da u njemu postoji polje „iznos povrata", klijent bi mogao
otkazati dan prije termina i sam upisati sto posto.

Redoslijed provjera je: je li razlog ispravan (400) → postoji li rezervacija (404) →
smijem li joj uopšte pristupiti (403) → smije li se otkazati (400) → koliko se vraća →
upis. Provjera razloga ide prva jer ne otkriva ništa o rezervaciji; vlasništvo ide odmah
poslije postojanja, da tuđi broj u adresi ne može izvući ni iznos ni razlog otkazivanja.

Testirano kroz API:

| Test | Rezultat |
|---|---|
| Lista za klijenta / za agenciju | šest / pet razloga, „Ostalo" i vremenski uslovi u obje |
| Otkazivanje bez razloga | 400, „Odaberite razlog otkazivanja." |
| Klijent bira razlog agencije | 400, „…može izabrati samo agencija." |
| „Ostalo" bez napomene | 400, „Uz razlog „Ostalo" upišite kratko objašnjenje." |
| Klijent otkazuje uz razlog iz liste | otkazano, razlog upisan |
| Agencija otkazuje uz „Ostalo" i napomenu | audit zapis „Ostalo - Poslovnica zatvorena zbog inventure.", izvršilac uposlenik |
| Brisanje korištenog razloga | 400, uz prijedlog da se razlog isključi |
| Klijent pokušava brisati razlog | 403 |

### Vozilo koje je već izdato se ne otkazuje

Ako po rezervaciji postoji `Primopredaja` tipa `Izdavanje`, otkazivanje se odbija.
Vozilo je kod klijenta i taj najam se ne poništava nego zatvara evidentiranjem povrata
vozila — put vodi u `Completed`, ne u `Cancelled`.

Isti izvor istine koristi i prikaz i upis: polje `mozeSeOtkazati` u obračunu i provjera
pri otkazivanju zovu istu funkciju. Zato se ne može desiti da dugme u aplikaciji bude
aktivno a zahtjev odbijen, ili obrnuto.

### Povrat se veže za plaćanje, ne za rezervaciju

Stripe povrat ide prema konkretnom `PaymentIntent`-u, pa se iznos raspoređuje po
plaćanjima umjesto da bude jedan slobodan zapis uz rezervaciju. Zapis nastaje sa
statusom `Created`: postoji, ali prema provajderu još nije poslan.

> ⬜ **Slanje povrata prema Stripe-u dolazi u fazi 12.** Do tada zapis stoji kao
> evidentirana obaveza, a ne kao izvršena isplata. Tek potvrda sa servera Stripe-a
> prevodi ga u `Succeeded`.

### Šta je testiranje otkrilo u seedu

Prvi prolaz kroz test pokazao je da posljednji zapis historije nije bio otkazivanje
nego starije „plaćanje verifikovano". Otkazivanje jeste bilo upisano — greška je bila
u seed podacima.

Seed je računao datum kreiranja kao `datumOd` minus jedan do dvadeset jedan dan. Za
termin tri sedmice unaprijed to zna završiti **u budućnosti**, pa je cijela historija
te rezervacije nosila datume koji još nisu nastupili i pri sortiranju po vremenu
ispadala iza stvarnog otkazivanja. Rezervacija nastala u budućnosti je podatak koji ne
bi izdržao nijedno pitanje, pa je datum kreiranja sada ograničen na prošlost.

Uz to je poredak historije učinjen stabilnim — `OrderBy(DatumVrijeme).ThenBy(Id)`. Bez
drugog ključa, dva zapisa u istom trenutku slaže baza kako joj odgovara, a to nije
poredak nego slučajnost koja se može razlikovati između dva poziva.

### Testovi kojima je korak zatvoren

21 novi unit test za pravila povrata, sa težištem na granicama: sedmi dan, treći dan i
trenutak kad je termin već počeo. Kroz API:

| Test | Rezultat |
|---|---|
| Obračun: povrat + zadržano = naplaćeno | ✅ 250,30 = 250,30 |
| Termin za 22 dana, klijent | 100 %, pun povrat |
| Klijent traži obračun za tuđu rezervaciju | 403 |
| Klijent otkazuje svoju | `Cancelled`, `DrziDo` prazno |
| Audit zapis nosi obrazloženje i iznos | „…najam se vraća u cijelosti. Povrat: 250,30 EUR." |
| Ponovno otkazivanje iste rezervacije | 400 |
| Obračun poslije otkazivanja | `mozeSeOtkazati=false`, već vraćeno 250,30, novi povrat 0,00 |
| Osoblje otkazuje bez razloga | 400 |
| Osoblje otkazuje uz razlog | `Cancelled`, izvršilac Emina Hodžić |
| Otkazivanje završene rezervacije | 400 |
| Nepostojeća rezervacija | 404 |

---

## Plaćanje i povrat novca

> ✅ Faza 12. Provjerena test skriptom nad pravim Stripe sandbox računom (rezultati na kraju sekcije).

Plaćanje ide kroz Stripe u test (sandbox) okruženju, valuta je EUR, a iznosi prema
Stripe-u putuju kao cijeli broj centi. Sve što dodiruje Stripe SDK stoji u jednoj
klasi, `StripeKlijent`; servis za plaćanje radi sa vlastitim zapisima
(`StripeIntent`, `StripePovrat`, `StripeDogadjaj`) i sadrži samo pravila.

### Tok

1. `POST /api/rezervacije/{id}/payment-intent` — samo klijent, samo za svoju
   rezervaciju. Tijela nema. Server provjeri da rezervacija čeka plaćanje i da
   držanje nije isteklo, uzme iznos iz rezervacije i napravi PaymentIntent.
   Klijent dobija `clientSecret` i javni ključ — ništa više.
2. Mobilna aplikacija otvara Stripe PaymentSheet. Plaćanje ostaje u aplikaciji:
   intent se pravi sa `AllowRedirects = "never"`, pa se ne nude načini plaćanja koji
   bi otvorili preglednik.
3. `POST /api/placanja/{id}/confirm` — aplikacija javlja da je PaymentSheet završio.
   Server to ne uzima kao dokaz, nego pita Stripe: status mora biti `succeeded`, a
   `AmountReceived` mora biti tačno iznos rezervacije u centima. Tek tada, u jednoj
   transakciji: plaćanje `Succeeded`, `NaplaceniIznos` iz Stripe odgovora,
   `IsPaid = true`, rezervacija `Pending → Confirmed` kroz state machine.
4. `POST /api/webhooks/stripe` — dodatni put do istog ishoda, kad Stripe sam javi.

Glavni put je treći, jer webhook traži javnu adresu, a nje u Docker okruženju pri
pregledu rada nema. Uputstvo dozvoljava „webhook ili server-side API verifikaciju";
urađeno je oboje, i oba puta završavaju u istoj metodi, `PrimijeniStanjeAsync`.
Pravilo o tome šta znači „plaćeno" postoji samo jednom.

### Zašto se iznos ne računa ponovo iz cjenovnika

Plan kaže da server pri kreiranju intenta računa iznos. Server ga i računa — ali pri
kreiranju rezervacije, kroz `PricingService`, i upisuje u `UkupanIznos`. Intent uzima
taj upisani broj. Ponovni obračun bi značio da izmjena cjenovnika između rezervacije i
plaćanja promijeni cijenu koju je klijent već prihvatio, a to specifikacija izričito
zabranjuje. Klijent ni u jednom od ta dva trenutka ne šalje iznos.

### Jedan intent, ne dva

Prije novog intenta servis pogleda postoji li otvoren (`Created` ili `Pending`). Ako
postoji, pita Stripe za njegovo stanje:

- ne postoji, poništen je ili glasi na drugi iznos → označi se `Canceled` i ide dalje
- već je naplaćen, samo potvrda nije stigla → završi se naplata odmah
- inače → vrati se isti `clientSecret`

Odbijena kartica ostavlja intent u stanju `requires_payment_method`, koje se kod nas
čuva kao `Created` — klijent smije pokušati drugom karticom istim intentom dok traje
držanje. Seed ima otvorene intente sa izmišljenim identifikatorima (`pi_seed_open_…`);
Stripe za njih kaže da ne postoje, pa se prvi pokušaj plaćanja takve rezervacije
uredno prebaci na novi intent.

### Idempotency ključ

`rez-{id}-v{redni broj}-{vrijeme kreiranja rezervacije}` za intent i
`povrat-{id}-{vrijeme kreiranja povrata}` za povrat.

Plan predviđa kraći oblik `rez-{id}-v{verzija}`. Vrijeme je dodano zato što Stripe
ključ pamti 24 sata, a identifikatori poslije `docker compose down -v` kreću od
jedinice. Bez toga bi nova rezervacija 5 dobila ključ stare rezervacije 5, sa drugim
iznosom, i Stripe bi zahtjev odbio kao zloupotrebu ključa.

### Kad novac stigne u pogrešnom trenutku

Tri slučaja koja se u praksi dese, i svaki ima odgovor:

| Situacija | Šta sistem radi |
|---|---|
| Klijent otkaže dok je PaymentSheet otvoren, pa plaćanje ipak prođe | plaćanje se bilježi kao uspjelo (novac je stigao), rezervacija ostaje otkazana, a cijeli iznos ide u povrat |
| Držanje istekne, pa plaćanje prođe | server zaključa vozilo i ponovo provjeri dostupnost; ako je termin slobodan, rezervacija se potvrđuje, ako ga je neko u međuvremenu uzeo, rezervacija se otkazuje uz pun povrat |
| Dva intenta iste rezervacije oba naplaćena | drugo plaćanje ne postaje `Succeeded` (to ni indeks ne bi dozvolio), a njegov iznos se vraća u cijelosti |

Zajedničko je jedno: novac koji je stigao se nikad ne ignoriše. Ili pripada
potvrđenoj rezervaciji, ili se vraća.

### Povrat: prvo zapis, pa Stripe

Otkazivanje u fazi 11 upisuje povrat u statusu `Created`. Sada ga `IzvrsilacPovrata`
šalje Stripe-u — ali tek **poslije** potvrde transakcije. Redoslijed je namjeran:

- Da se novac šalje prije potvrde, a transakcija zatim padne, klijent bi dobio povrat
  za otkazivanje koje ne postoji.
- Ovako je odluka već trajno upisana. Ako Stripe ne odgovori, otkazivanje i dalje
  važi, a povrat stoji zapisan i može se poslati ponovo.

Ishod slanja se razlikuje po tome šta se zna:

| Stripe je… | Status povrata | Šta dalje |
|---|---|---|
| prihvatio | `Succeeded` ili `Pending`, uz `re_…` identifikator | ništa |
| odbio (odgovor 4xx) | `Failed` | osoblje ga ponavlja; nastaje **novi** zapis sa novim ključem, a odbijeni ostaje u historiji |
| nije odgovorio (mreža, 5xx) | ostaje `Created` | ponavlja se **isti** zapis, istim ključem — ako je Stripe povrat ipak izvršio, vratit će isti rezultat umjesto drugog povrata |

`IzvrsilacPovrata` ne poziva `SaveChangesAsync`. Mijenja entitete koje dobije, a snima
servis koji vodi operaciju, nad istim `DbContext`-om. Uputstvo (3.4) traži da se
izbjegavaju servisi koji iz drugog servisa sami snimaju.

Otkazivanjem se poništavaju i otvoreni intenti, da se otkazana rezervacija ne može
naplatiti. Ako poništavanje ne uspije zato što je naplata upravo prošla, prvi red
tabele iznad preuzima stvar.

Osoblje ponavlja povrat kroz `POST /api/placanja/povrati/{id}/ponovi`, a listu
plaćanja sa odbijenim povratima dobija filterom `samoNeuspjeliPovrati=true`.

### Webhook bez `[AllowAnonymous]`

Stripe ne može poslati naš JWT, a uputstvo dozvoljava `[AllowAnonymous]` isključivo
na prijavi i registraciji — i posebno kaže da write operacije nikad ne smiju biti
otvorene. Webhook je upravo takva operacija.

Rješenje je druga autentifikacijska shema, `StripePotpis`. `StripePotpisHandler`
pročita tijelo, provjeri potpis iz zaglavlja `Stripe-Signature` prema tajni iz `.env`
fajla i tek tada zahtjevu dodijeli ulogu `StripeWebhook`. Kontroler traži baš tu shemu
i tu ulogu. Bez potpisa, ili sa lažnim, ASP.NET vraća 401 prije nego zahtjev dođe do
kontrolera. JWT ostaje podrazumijevana shema za sve ostalo.

U servisu webhook radi ovim redom:

1. potpis se provjerava još jednom, pri čitanju događaja
2. ako `ProviderEventId` već postoji u `ObradjeniWebhookEvent` → 200, bez efekata
3. transakcija, zaključavanje rezervacije
4. **stanje intenta se čita iz Stripe-a**, ne iz događaja — događaji znaju stići van
   reda, pa stariji `payment_failed` ne smije pregaziti noviji `succeeded`
5. isti `PrimijeniStanjeAsync` kao kod potvrde
6. upis događaja u istoj transakciji; ako isti događaj istovremeno stigne dvaput,
   jedinstveni indeks odbije drugi upis i ta transakcija se poništava

Lokalno se webhook testira kroz Stripe CLI:

```
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

Komanda ispiše `whsec_…`, što ide u `STRIPE_WEBHOOK_SECRET`.

### Konfiguracija

`StripePostavke` čita tri ključa iz `.env` jednom, pri pokretanju. API se podiže i bez
njih — ostatak sistema radi, a endpointi za plaćanje odgovaraju porukom šta nedostaje.
Live ključ (`sk_live_…`) obara pokretanje: uputstvo traži sandbox, a slučajno
pokretanje sa pravim ključem značilo bi prave naplate.

Stripe klijent dobija `HttpClient` iz `IHttpClientFactory`, ne kroz `new HttpClient()`
(uputstvo 3.4). Javni ključ klijent dobija od servera, uz intent, pa ga mobilna
aplikacija ne mora imati upisanog.

### Šta još nije povezano

- ✅ Poruka `placanje.uspjesno` i email potvrde su tu od faze 14a. Objavljuje se samo
  za ishod „potvrđeno", pa ponovljena potvrda ne šalje drugi email.
- ✅ Notifikacija klijentu — faza 15, kroz `NotifikacijaService` i SignalR hub.
- ✅ Povrat depozita pri vraćanju vozila — faza 13, kroz isti `IzvrsilacPovrata`.
- ✅ Periodično ponovno slanje povrata koji su ostali `Created` — faza 14b, u workeru.

### Testovi kojima je korak zatvoren

30 novih unit testova za pretvaranje iznosa i statusa (`IznosiStripeTests`) —
ukupno 116. Kroz API, skriptom `test-faza12.ps1`:

| Test | Rezultat |
|---|---|
| Klijent traži intent | `clientSecret`, 207,80 EUR iz rezervacije, držanje 899 s |
| Ponovni zahtjev | isti `placanjeId` (357 = 357) |
| Potvrda prije plaćanja | 400, „Plaćanje nije izvršeno…" |
| Uposlenik traži intent / tuđi klijent čita plaćanje | 403 / 403 |
| Kartica 4242, prije potvrde | Stripe `succeeded`, 20780 centi — rezervacija i dalje `Pending`, `isPaid=false` |
| Potvrda | `Succeeded`, naplaćeno 207,80, `Confirmed`, `isPaid=true`, audit zapis „Plaćanje verifikovano na serveru" |
| Ponovna potvrda | 200, zapisa u historiji 2 → 2 |
| Intent za plaćenu rezervaciju | 400, „Rezervacija je već plaćena." |
| Odbijena kartica | potvrda 400, novi pokušaj koristi isti intent (358 = 358) |
| Otkazivanje plaćene | povrat 207,80 `Succeeded`, `re_3UGeKT…` |
| Otkazivanje neplaćene | otvoren intent `Canceled` |
| Webhook bez potpisa / sa lažnim | 401 / 401 |

Naplate, odbijena kartica i povrat vidljivi su i u Stripe dashboardu (test način).

Webhook je zatim provjeren i sa pravim događajima, kroz `stripe listen`, uz istu
skriptu. Svih 13 događaja je dobilo 200. Ono što je vrijedno vidjeti u logu:

| Događaj | Šta je server uradio |
|---|---|
| `payment_intent.succeeded` | stigao poslije serverske potvrde iz aplikacije → ishod `VecObradjeno`, bez drugog prelaza statusa |
| `payment_intent.payment_failed` | odbijena kartica → plaćanje ostaje otvoreno za novi pokušaj |
| `payment_intent.canceled` | poslije otkazivanja neplaćene rezervacije → stanje se ne mijenja, već je `Canceled` |
| `refund.created`, `refund.updated` | povrat već `Succeeded` iz odgovora na zahtjev → status potvrđen |
| `charge.*`, `payment_intent.created` | evidentirani kao obrađeni, bez ikakvog efekta |

Oba puta — potvrda iz aplikacije i webhook — stigla su za isto plaćanje u razmaku od
sekunde, a rezervacija je prešla u `Confirmed` tačno jednom.

---

## Izdavanje i povrat vozila

> ✅ Faza 13.

Rezervacija ima četiri statusa i izdavanje vozila nije jedan od njih. Kad klijent
preuzme vozilo, rezervacija ostaje `Confirmed`, a da je vozilo kod njega vidi se po
tome što postoji zapis `Primopredaja` tipa `Izdavanje`, a nema zapisa tipa `Povrat`.
Tek povrat mijenja status, u `Completed`, i to kroz state machine.

Endpointi su pod `/api/primopredaje`:

| Endpoint | Ko | Šta radi |
|---|---|---|
| `GET /` i `GET /{id}` | svi prijavljeni | osoblje vidi sve, klijent samo primopredaje svojih rezervacija |
| `POST /izdavanje` | osoblje | izdavanje, podaci i fotografije u jednoj multipart formi |
| `POST /povrat` | osoblje | povrat sa obračunom depozita |
| `GET /obracun-povrata/{rezervacijaId}` | osoblje | obračun za formu povrata, ništa ne upisuje |
| `GET /raspored` | osoblje | preuzimanja i vraćanja zakazana za dan |
| `GET /fotografije/{id}` | svi prijavljeni | preuzimanje fotografije, uz provjeru vlasništva |

### Izdavanje

Provjere redom:

1. Uposlenik je označio da je prošao kontrolnu listu stanja vozila → inače 400
2. Najviše deset fotografija
3. Zaključavanje reda rezervacije
4. Rezervacija je `Confirmed` i plaćena
5. Vozilo za nju još nije izdato, i povrat nije već evidentiran
6. Vrijeme izdavanja nije ranije od dva sata prije termina (koliko traje i priprema
   vozila), nije poslije ugovorenog vraćanja i nije u budućnosti
7. Kilometraža nije manja od zadnje evidentirane na vozilu

Uposlenika upisuje server, iz tokena. Kilometraža vozila se ažurira, pa sljedeće
izdavanje ima s čim da poredi.

### Kad se desilo i kad je upisano nisu isto

Zapis `Primopredaja` ima dva vremena: `DatumVrijeme` je kad se primopredaja stvarno
desila, `DatumUnosa` kad je zapis nastao. Obično su isti, jer se oba dese na šalteru.

Razlikuju se kad uposlenik naknadno evidentira nešto što je propustio — najčešće
izdavanje, kojeg se sjeti tek kad klijent dođe vratiti vozilo. Zahtjevi zato imaju
opcione `DatumIzdavanja` i `DatumPovrata`; prazno znači „sada".

Dva polja a ne jedno, jer o svakom ovisi nešto drugo. **O vremenu događaja ovisi
novac**: kašnjenje pri povratu se računa iz njega i umanjuje povrat depozita, pa bi
klijent koji je vozilo vratio na vrijeme platio kašnjenje uposlenika koji je unos
odgodio. **O vremenu unosa ovisi trag**: ko je i kada šta upisao. Kad bi postojalo
samo jedno polje, naknadni unos bi morao ili falsifikovati trenutak unosa ili
naplatiti kašnjenje koje se nije desilo.

Vrijeme događaja nije slobodno: ne smije biti u budućnosti (uz dvije minute
tolerancije, jer sat na računaru uposlenika nije tačan u sekundu), izdavanje mora pasti
unutar ugovorenog termina, a povrat ne prije izdavanja. `PrimopredajaDto` vraća
`unesenoNaknadno`, pa se u pregledu vidi da zapis nije nastao u trenutku događaja.

### Struja nije gorivo

Primopredaja u oba slučaja bilježi isti broj — procenat — jer je to ista mjera: koliko
je ostalo od punog. Ali skuter na struju nema rezervoar, pa je natpis „nivo goriva" na
formi koju uposlenik ispunjava jednostavno netačan.

Da bi aplikacija znala razliku, tip goriva u šifarniku ima zastavicu `JeElektricni`.
Namjerno **nije** poređenje naziva u kodu (`Naziv == "Elektricni"`): naziv je podatak
koji administrator može preimenovati ili dopuniti novim zapisom („Struja", „EV"), pa bi
provjera po nazivu tiho prestala važiti, a niko to ne bi primijetio dok neko ne pogleda
formu. Ovako svojstvo stoji uz zapis koji ga opisuje.

Zastavica se prenosi kroz `VoziloDto`, `ModelVozilaDto` i `RasporedStavkaDto`, pa
ekran ne mora ništa zaključivati: klizač piše „Napunjenost baterije" s ikonom baterije,
a lista vozila prikazuje snagu u kW umjesto kubikaže, koja za takva vozila ne postoji.

### Raspored zna šta se po redu može uraditi

`RasporedStavkaDto` uz `Obavljeno` nosi i `IzdavanjeEvidentirano`. Bez toga red za
vraćanje izgleda isto bilo da vozilo jeste ili nije izdato, a razlika je velika: povrat
bez izdavanja server odbija. Desktop zato takvom redu nudi „Evidentiraj izdavanje"
umjesto „Zaprimi vozilo", pa uposlenik iz istog reda riješi ono što je propušteno.

Seed prati isto pravilo: najam koji je u toku — potvrđen, a preuzimanje mu je prošlo —
dobija zapis o izdavanju bez povrata. Ranije su primopredaje nastajale samo za završene
rezervacije, pa je u demo podacima postojao najam u toku bez ijednog traga da je vozilo
predato klijentu.

### Povrat i obračun depozita

Obračun je u čistoj klasi `ObracunPovrata`, bez baze, i pokriven je testovima:

```
kašnjenje          = vrijeme povrata − ugovoreno vraćanje  (ne manje od nule)
do 59 minuta       → ne naplaćuje se
preko toga         → svaki započeti dan preko tolerancije je cijeli dan
doplata            = dani × dnevna cijena (tarifa i sezonski množilac na datum vraćanja)
za naplatu         = šteta + doplata
zadržano           = manje od (depozit, za naplatu)
povrat depozita    = depozit − zadržano
nepokriveno        = za naplatu − zadržano
```

Kašnjenje se broji istim pravilom kao trajanje najma. Da ovdje važi drugo pravilo,
klijent bi za isto prekoračenje platio različito zavisno od toga da li je najam
produžio ili samo kasnio. Dnevnu cijenu daje `PricingService`, jer je on jedino
mjesto gdje se računa cijena.

**Depozit** je dio naplate koji je stvarno kod agencije: manji od
`Rezervacija.IznosDepozita` i od naplaćenog iznosa umanjenog za već vraćeno. Računa se
iz `NaplaceniIznos`, ne iz cjenovnika.

**Nepokriveno** je šteta koja premašuje depozit. Sistem je prikazuje, ali je ne
naplaćuje — to agencija rješava s klijentom mimo aplikacije. To je svjesna granica
sistema, ne previd.

Isti obračun radi i `GET /obracun-povrata` (za formu, uz opcione `datumPovrata` i
`iznosStete`) i sam povrat, pa uposlenik prije potvrde vidi tačno ono što će se desiti.
Odgovor uz obračun nosi i podatke sa izdavanja — kilometražu, gorivo, ko je izdao i
broj fotografija — za poređenje na formi.

Provjere pri povratu:

1. Ako je označeno oštećenje: opis (bar 5 znakova), iznos veći od nule i **najmanje
   jedna fotografija**. Bez fotografije šteta nema dokaz, a iznos se odbija od
   klijentovog novca. Bez oznake oštećenja opis i iznos se ne primaju.
2. Datum blokade, ako je naveden, mora biti u budućnosti
3. Zaključavanje reda rezervacije
4. Rezervacija je `Confirmed` i plaćena, vozilo je izdato, a povrat još nije evidentiran
5. Kilometraža nije manja nego pri izdavanju

Zatim, u jednoj transakciji: zapis o povratu i eventualna `EvidencijaStete`, nova
kilometraža vozila, prelaz `Confirmed → Completed` sa cijelim obračunom u audit
zapisu, povrat depozita u statusu `Created` i, ako je uposlenik tražio, blokada vozila
do navedenog datuma — prijava predviđa da uposlenik može odmah blokirati vozilo u
kvaru. Povrat depozita ide Stripe-u tek poslije potvrde transakcije, istim putem kao
povrat pri otkazivanju.

Fotografije se snimaju prije upisa u bazu, jer se tek tada zna da su ispravne slike
(magic bytes). Ako upis ne uspije, snimljeni fajlovi se brišu.

### Fotografije su privatne

Fotografije primopredaje idu u privatni folder, kao i fotografije vozačkih dozvola.
U bazi je ključ, ne adresa, a thumbnail se ne pravi. Klijent smije preuzeti samo
fotografije sa svojih rezervacija. `PrimopredajaDto` nosi samo identifikatore
fotografija.

Zato je iz `FotografijaPrimopredaje` uklonjena kolona `PutanjaThumbnail`, a seed
primopredaje sada pokazuju na privatnu placeholder sliku umjesto na javne slike
vozila.

### Raspored za dan

`GET /api/primopredaje/raspored` vraća potvrđene i završene rezervacije kojima
preuzimanje ili vraćanje pada u zadati period, sa oznakom da li je primopredaja već
obavljena. Aplikacija šalje `od` i `do` u UTC-u, jer „danas" zavisi od vremenske
zone. Bez njih server uzima današnji dan po UTC-u. Period je najviše sedam dana, a
lista je paginirana kao i sve ostale.

### Šta ova faza namjerno ne radi

- Raniji povrat vozila ne vraća novac za neiskorištene dane. Politika to ne predviđa.
- Šteta preko depozita se ne naplaćuje kroz sistem (vidi gore).
- ✅ Email i notifikacija o povratu depozita idu kroz `povrat.izvrsen` od faze 14a.

### Testovi kojima je korak zatvoren

17 novih unit testova za obračun depozita (`ObracunPovrataTests`), sa granicama
tolerancije na 59 i 60 minuta — ukupno 133. Kroz API, na rezervaciji plaćenoj preko
Stripe sandboxa (192,90 EUR, depozit 150 EUR):

| Test | Rezultat |
|---|---|
| Obračun unaprijed, 30 min kašnjenja | u toleranciji, 0 dana, povrat depozita 150,00 |
| Obračun unaprijed, 25 h kašnjenja i šteta 50 | 2 dana × 42,90 = 85,80, zadržano 135,80, povrat 14,20 |
| Povrat prije izdavanja | 400 |
| Izdavanje bez kontrolne liste | 400 |
| Kilometraža manja od zadnje na vozilu | 400, „…zadnje evidentirane (32455 km)" |
| Tekstualni fajl sa `.jpg` ekstenzijom | 400, magic bytes ga odbijaju |
| Klijent pokušava izdati vozilo | 403 |
| Izdavanje sa dvije fotografije | 200, status rezervacije ostaje `Confirmed` |
| Drugo izdavanje / otkazivanje izdate rezervacije | 400 / 400 |
| Raspored za dan | preuzimanje obavljeno, vraćanje na čekanju |
| Oštećenje bez fotografije / bez iznosa / iznos bez oznake | 400 / 400 / 400 |
| Kilometraža manja nego pri izdavanju | 400 |
| Povrat sa štetom 50 i blokadom | `Completed`, audit zapis sa obračunom, povrat depozita 100,00 `Succeeded` kod Stripe-a, blokada vozila na tri dana |
| Drugi povrat | 400 |
| Vlasnik preuzima fotografiju / tuđi klijent / bez tokena | 200 / 403 / 401 |
| Direktna adresa privatnog foldera | 404 |
| Tuđi klijent u listi primopredaja | 0 zapisa |

---

## Poruke, worker i email

> 🟢 Faza 14 je gotova: poruke i emailovi (14a), periodični poslovi i reset lozinke (14b).

Uputstvo (sekcija 3.2) traži najmanje dva servisa i izričito kaže da `BackgroundService`
unutar API projekta ne zadovoljava taj zahtjev, jer radi u istom procesu. Zato je
`SunnyRides.Subscriber` zaseban projekat, sa vlastitim `Dockerfile`-om i vlastitim
kontejnerom, koji ne izlaže nijedan HTTP endpoint.

### Ko šalje, ko sluša

| Red | Šalje | Worker radi |
|---|---|---|
| `rezervacija.kreirana` | `RezervacijaService` poslije upisa | email sa uputom za plaćanje i preostalim vremenom držanja |
| `placanje.uspjesno` | `PlacanjeService`, samo kad je ishod `Potvrdjeno` | email potvrde sa detaljima preuzimanja |
| `rezervacija.otkazana` | `RezervacijaService` poslije otkazivanja | email sa razlogom otkazivanja |
| `povrat.izvrsen` | `IzvrsilacPovrata`, kad Stripe prihvati povrat | email sa iznosom povrata |
| `dozvola.verifikovana` | `DozvolaService` kod odobrenja i odbijanja | email o ishodu, sa razlogom kad je odbijena |
| `vozilo.vraceno` | `PrimopredajaService` poslije evidentiranog povrata | email sa obračunom depozita i zatvaranjem najma |
| `podsjetnik.preuzimanje` | periodični posao u workeru | podsjetnik 24 h prije preuzimanja |
| `reset.lozinke` | `AuthService` kad neko zatraži novu lozinku | email sa kodom koji vrijedi 15 min |

Posljednja dva reda znače da worker šalje poruke i sam sebi: periodični posao objavi
poruku, a potrošač je pokupi iz reda kao i svaku drugu. Moglo je i kraće — posao bi
mogao odmah poslati email — ali onda bi slanje emaila postojalo na dva mjesta, sa dva
puta kroz koja se greška obrađuje drugačije.

Uz svaki email worker upisuje i `Notifikacija` zapis — ne više direktno kroz `DbContext`
nego kroz `NotifikacijaService`, koji uz upis objavi poruku za guranje na uređaj. Detalji
su u sekciji „Notifikacije i SignalR".

### Poruka nosi identifikator, ne tekst

Poruke su male: `{ "rezervacijaId": 371 }`. Worker sve ostalo pročita iz baze u trenutku
obrade. Da poruka nosi gotov tekst emaila, obrada minutu kasnije slala bi podatke koji
su možda već zastarjeli — a i svaka izmjena teksta tražila bi izmjenu na dva mjesta.

Ugovor (nazivi redova i oblik poruka) živi u `SunnyRides.Model`, jer ga koriste i API
koji šalje i worker koji sluša. Da svaka strana ima svoju kopiju, prvi preimenovan red
bio bi tiha greška: poruke bi odlazile u red koji niko ne sluša.

### Kad se poruka šalje

Uvijek **poslije potvrđene transakcije**, nikad unutar nje. Obrnuto bi značilo da se
email o plaćanju pošalje za transakciju koja se u međuvremenu poništila, a to se ne
može povući.

Objavljivač namjerno ne baca izuzetke. Ako je broker nedostupan, zahtjev korisnika je
već uspio i ne smije pasti zbog emaila; greška se logira sa cijelim sadržajem poruke.
Konekcija prema brokeru je jedna za cijelu aplikaciju (singleton), a kanal se otvara po
objavi, jer kanali nisu sigurni za istovremeno korištenje iz više niti. Otvaranje nove
konekcije po poruci uputstvo navodi kao grešku.

### Kako worker obrađuje

- `AsyncEventingBasicConsumer`, kako uputstvo traži.
- `BasicQos` sa `prefetchCount = 1`: sljedeća poruka se uzima tek kad je prethodna
  potvrđena.
- `autoAck = false`: poruka se briše iz reda tek kad je posao stvarno obavljen. Da je
  `true`, pad workera značio bi tiho izgubljen email.
- Neuspjela obrada se ponavlja **1 s → 2 s → 4 s → 8 s**. Ako ni zadnji pokušaj ne
  uspije, poruka se odbacuje bez vraćanja u red i logira se u cijelosti. Vraćanje u red
  bi je vrtjelo u krug i zaglavilo sve poruke iza nje.
- Nijedna greška se ne guta u praznom `catch` bloku.
- Povezivanje na broker se ponavlja sa sve dužim razmakom, jer se worker u Dockeru
  pokreće u isto vrijeme kad i RabbitMQ.

Poruka koja se ponovo isporuči poslije pada workera može donijeti drugi email za isti
događaj. To je svjesno prihvaćeno: bolje dva emaila nego nijedan, a stanje u bazi se
time ne kvari jer worker samo dodaje notifikaciju.

### Email

MailKit, SMTP podaci iz `.env`. Konekcija se otvara po poruci i uredno zatvara — za
nekoliko emaila u minuti to je jeftinije od držanja otvorene sesije koju server ionako
zatvori.

Kad SMTP podaci nisu postavljeni, worker i dalje radi: notifikacije se upisuju, a u log
se upiše šta bi bilo poslano. Tako se sve ostalo može testirati bez SMTP naloga.

**Demo podaci ne smiju slati poštu stvarnim ljudima.** Ovo je bio stvarni propust:
seed je izmišljenim klijentima davao `@gmail.com` adrese, pa je worker, kad je počeo
slati emailove, poslao poruku na adresu koja lako pripada nekoj stvarnoj osobi.
Riješeno je u dva sloja:

1. **Adrese u seed podacima su na rezervisanim domenama** — `example.com` za klijente i
   `sunnyrides.example` za osoblje. Standard (RFC 2606) te domene drži po strani baš za
   primjere i one ne mogu pripasti nikome, pa poruka nema kome da stigne.
2. **`SMTP_PREUSMJERI_NA` u `.env`** — kad je postavljeno, svaki email ide na tu adresu
   umjesto na korisnikovu, a u naslovu stoji `[za <stvarna adresa>]`. Tako se cijeli
   tok može ispratiti na vlastitom sandučetu, a adresa unesena greškom ne može nikome
   otići. U pravom radu polje ostaje prazno.

### Periodični poslovi

Pored slušanja redova, worker ima i četiri posla koje pokreće sat. Svaki nasljeđuje
`PeriodicniPosao`, koji rješava ono što im je zajedničko: **svoj opseg po prolazu**
(jer je `DbContext` Scoped i ne smije živjeti koliko i cijeli proces) i **greška koja
ne obara posao** — prolaz koji padne se zabilježi, a sljedeći ide normalno.

| Posao | Razmak | Šta radi |
|---|---|---|
| `OtkazivanjeNeplacenih` | 1 min | otkazuje rezervacije kojima je isteklo držanje, poništava otvoreni intent, javlja klijentu |
| `PodsjetniciZaPreuzimanje` | 15 min | objavljuje `podsjetnik.preuzimanje` za preuzimanja u narednih 24 h |
| `PonovnoSlanjePovrata` | 5 min | šalje povrate koji su zapisani, a nisu stigli do Stripe-a |
| `CiscenjeIsteklihZapisa` | 6 h | briše istekle opozvane tokene i istekle kodove za reset |

**Zašto otkazivanje uopšte treba.** Pretraga neplaćenu rezervaciju prestaje računati
kao zauzeće čim `DrziDo` prođe, pa vozilo nije blokirano ni prije nego posao stigne.
Ali rezervacija ne smije ostati vječno „na čekanju": klijent u svom pregledu mora
vidjeti da je istekla, a otvoreni `PaymentIntent` mora biti poništen — inače bi se
termin koji više ne važi mogao naplatiti.

**Kako se ne sudari sa plaćanjem.** Posao zaključava isti red (`UPDLOCK, HOLDLOCK`)
kojim se zaključava i potvrda plaćanja, pa **ponovo pročita stanje poslije
zaključavanja**. Ako je klijent platio u međuvremenu, uslov više ne vrijedi i posao
odustaje. Ako plaćanje stigne poslije otkazivanja, `PlacanjeService` ga zatekne kao
otkazanu rezervaciju i vrati cijeli iznos — to je već opisano u sekciji o plaćanju.

**Ko je otkazao.** Niko. `OtkazaoKorisnikId` ostaje prazan, a audit zapis nema
izvršioca, jer ga stvarno nije bilo. `SistemskiKorisnik` u workeru je implementacija
`ICurrentUserService` koja na sve odgovara prazno — umjesto izmišljenog „sistemskog
naloga" koji bi u historiji izgledao kao da je neko pritisnuo dugme. Status i ovdje
mijenja ista `RezervacijaStateMachine` koju koristi API: automatsko otkazivanje ne
smije biti drugo pravilo od ručnog.

Razlog otkazivanja je šifrarnički zapis **„Isteklo vrijeme za plaćanje"**, koji nije
aktivan i ne nudi se ni klijentu ni agenciji, pa ga niko ne može izabrati ručno.
Postoji samo da otkazana rezervacija ima čime objasniti zašto je otkazana — i u
pregledu i u emailu.

**Podsjetnici bez nove kolone.** Šta je poslano zna se po `Notifikacija` zapisu koji
za tu rezervaciju već stoji u bazi; obrada poruke ga upisuje prije nego što krene
slanje emaila. Trag o poslatom podsjetniku tako postoji tamo gdje mu je i inače
mjesto, u notifikacijama koje klijent vidi u aplikaciji.

**Ponovno slanje povrata** ide **istim idempotency ključem**, vezanim za zapis
povrata. Ako je Stripe prvi zahtjev ipak primio, vratiće isti povrat umjesto da napravi
novi — pa se novac ne može vratiti dvaput. Dira se samo povrat stariji od dvije minute;
mlađi je vjerovatno upravo u slanju iz API-ja.

**Čišćenje** briše opozvane tokene kojima je rok ionako istekao (od tada ih odbija
provjera roka), i kodove za reset dan poslije isteka. Tabela kroz koju prolazi svaki
zahtjev ne smije rasti zauvijek.

### Reset zaboravljene lozinke

Dva koraka, oba `[AllowAnonymous]`, jer korisnik koji ne zna lozinku ne može doći do
tokena:

1. `POST /api/auth/zaboravljena-lozinka` — prima email, uvijek vraća **204**.
2. `POST /api/auth/reset-lozinke` — prima email, kod i novu lozinku.

Šta je urađeno da ovaj put ne postane rupa:

- **Kod se čuva hashiran** (BCrypt), isto kao lozinka. U bazi ne stoji ništa čime bi se
  nalog mogao otvoriti — ni onome ko bazu vidi. U čitljivom obliku kod postoji samo u
  poruci i u emailu.
- **Kod se generiše `RandomNumberGenerator`-om**, nikad `System.Random`-om: Random je
  predvidiv, a ovdje ta vrijednost otvara tuđi nalog.
- **Osam znakova iz abecede od 32** daje preko 10¹² kombinacija. Šestocifreni PIN, koji
  se obično viđa, ima ih milion — a milion pokušaja kroz API je izvodljivo. Abeceda je
  bez `I`, `O`, `0` i `1`, jer se pri prepisivanju miješaju.
- **Vrijedi 15 minuta i jednom.** Novi zahtjev poništava sve ranije kodove istog
  korisnika, pa važeći kod je uvijek tačno jedan — onaj iz posljednjeg emaila.
- **Odgovor je isti bez obzira na to postoji li nalog** sa tom adresom. Da nije tako,
  ovaj endpoint bio bi besplatna provjera koje su adrese registrovane.
- **Poruka o grešci je jedna jedina**: nepostojeći nalog, pogrešan kod i istekao kod
  izgledaju identično. Različite poruke bi rekle napadaču kada je pogodio email, a kada
  kod.
- Kod ide **isključivo u email**. Notifikacija u aplikaciji samo kaže da je kod poslan,
  da ga ne bi vidio neko ko je na tuđem uređaju otvorio spisak obavještenja.
- **Kod ne ide u log.** Objavljivač i potrošač inače zapisuju cijeli sadržaj poruke, što
  je korisno kad se posao mora ponoviti ručno — ali red `reset.lozinke` je označen kao
  tajni (`Redovi.SadrziTajnu`) i za njega se logira samo naziv reda. Log čita više ljudi
  nego bazu i najčešće se čuva duže nego što kod važi.

**Otvoreno pitanje za mentora.** Uputstvo kaže da `[AllowAnonymous]` smije stajati samo
na prijavi i registraciji. Reset zaboravljene lozinke po prirodi stvari ne može tražiti
token — korisnik do njega ne može doći. Implementiran je sa mjerama iznad; ako se traži
doslovno poštovanje pravila, funkcionalnost se uklanja iz mobilne aplikacije i lozinku
resetuje administrator kroz upravljanje korisnicima.

**Šta ovaj korak namjerno ne radi.** Postojeći tokeni se poslije promjene lozinke ne
poništavaju — za to bi u `Korisnik` trebalo polje „tokeni izdati prije ovog trenutka ne
važe", a odjava trenutno radi po `jti`-ju pojedinačnog tokena. Zapisano kao poznato
ograničenje.

---

## Notifikacije i SignalR

> 🟢 Faza 15 je gotova: endpointi za obavještenja, hub i isporuka u realnom vremenu.

Uputstvo traži da se lista obavještenja osvježava sama i izričito kaže da ručni refresh
nije prihvatljiv. To znači da server mora moći progovoriti prvi, a HTTP to ne može —
zahtjev uvijek počinje klijent. Zato SignalR.

### Gdje obavještenje nastaje

Do sada ih je upisivao worker, direktno kroz `DbContext`. Sada postoji
`NotifikacijaService` i on je jedino mjesto na kojem obavještenje nastaje:

```
worker (obrada poruke)  ->  NotifikacijaService.KreirajAsync
                                |
                                +-- upis reda u Notifikacija
                                +-- objava na razmjenu "notifikacije"
                                             |
                       API (SlusacNotifikacija) <-+
                                |
                                +-- NotifikacijaHub -> grupa "korisnik-{id}"
```

Razlog za jedno mjesto je praktičan: kad bi upis ostao raspoređen po pozivaocima, prvi
koji zaboravi objaviti poruku dao bi obavještenje koje se pojavi tek pri sljedećem
otvaranju aplikacije — a to je tačno ono ponašanje koje faza treba ukloniti.

### Zašto razmjena, a ne red

Redovi u ovom sistemu imaju jednog čitaoca: poruku uzme prvi ko stigne i ona je time
potrošena. To je ispravno za posao koji se radi jednom, kao slanje emaila.

Guranje na uređaj traži suprotno. Veze prema uređajima drži svaka pokrenuta instanca
API-ja za sebe, pa poruku mora dobiti **svaka** instanca, a ne bilo koja. Zato
`Razmjene.Notifikacije` nije red nego fanout razmjena: svaka instanca sebi pravi
privatni red bez naziva (`exclusive`, `autoDelete`), veže ga na razmjenu i dobija kopiju
svake poruke. Kad se instanca ugasi, red nestaje s njom — da ostane, u njemu bi se
gomilale poruke koje niko neće pročitati.

Ove poruke namjerno **ne** preživljavaju restart brokera i idu sa `autoAck: true`.
Isporuka u realnom vremenu ima smisla samo dok je događaj svjež; sam zapis je u bazi i
aplikacija ga pokupi pri sljedećem otvaranju liste. Zato se ovdje ne ponavlja ono što
se u redovima ponavlja četiri puta.

### Zašto slušalac stoji u API projektu

`SlusacNotifikacija` je `BackgroundService` unutar API-ja, što na prvi pogled liči na
ono što uputstvo zabranjuje. Nije isto: zabrana se odnosi na *obavljanje posla* u API
procesu umjesto u zasebnom servisu, a posao je ovdje već obavljen u workeru. U API-ju
se rezultat samo isporučuje, i to mora biti baš tu — SignalR poruka se ne može poslati
iz procesa koji ne drži vezu.

### Hub

`NotifikacijaHub` nema nijednu metodu koju klijent može pozvati. Veza se pri uspostavi
sama svrstava u grupu `korisnik-{id}`, a identifikator se čita iz tokena. Da hub ima
metodu tipa „prijavi me na korisnika X", bilo bi dovoljno poslati tuđi broj i slušati
tuđa obavještenja — i to se ne bi vidjelo ni u jednom logu HTTP zahtjeva.

Token za hub stiže kao `access_token` u query stringu, jer WebSocket zahtjev ne nosi
`Authorization` zaglavlje. To vrijedi **samo** za putanju huba (`/hubs/notifikacije`);
da uslova nema, svaki endpoint bi prihvatao token iz adrese, a adrese završavaju u
logovima i historiji preglednika.

Hub je mapiran poslije `UseAuthentication` i middlewarea za opozvane tokene, pa token
poništen odjavom ne otvara vezu.

### Šta hub šalje

| Poruka | Sadržaj |
|---|---|
| `NovaNotifikacija` | `NotifikacijaDto`, isti oblik koji vraća i lista |
| `BrojNeprocitanih` | `{ broj }`, da aplikacija zbog oznake na zvonu ne mora raditi još jedan zahtjev |

### Endpointi

| Metoda | Ruta | Šta radi |
|---|---|---|
| GET | `/api/notifikacije` | lista sa paginacijom i pretragom po pročitanom, tipu, rezervaciji, tekstu i periodu |
| GET | `/api/notifikacije/{id}` | jedno obavještenje |
| GET | `/api/notifikacije/broj-neprocitanih` | broj za oznaku na zvonu |
| POST | `/api/notifikacije/{id}/procitaj` | označava pročitanim; ponovljen poziv vraća isto stanje |
| POST | `/api/notifikacije/procitaj-sve` | označava sve pročitanim, jednim upitom nad bazom |

Nijedna ruta ne prima identifikator korisnika. Lista je sužena u `AddFilter`, a
pojedinačno obavještenje se prije odgovora poredi sa vlasnikom iz tokena — inače bi
bilo dovoljno mijenjati broj u adresi da se čitaju tuđa obavještenja, a u njima stoje
brojevi rezervacija i iznosi.

Za razliku od rezervacija, ovdje **nema** izuzetka za osoblje. Obavještenje je lična
poruka i uposlenik nema razloga čitati tuđa.

Obavještenja se ne unose i ne brišu kroz API. Nastaju isključivo kao posljedica
događaja u sistemu, pa bi endpoint za unos imao ko da ga zove samo ako neko želi tuđem
korisniku poslati poruku.

### Za koje događaje obavještenje postoji

| Tip | Događaj |
|---|---|
| `RezervacijaKreirana` | rezervacija upisana, čeka plaćanje |
| `PlacanjeUspjesno` | plaćanje potvrđeno na serveru, rezervacija potvrđena |
| `RezervacijaOtkazana` | otkazivanje, sa razlogom |
| `PovratIzvrsen` | povrat novca prihvaćen kod Stripe-a |
| `DozvolaOdobrena` / `DozvolaOdbijena` | uposlenik verifikovao dozvolu |
| `PodsjetnikPreuzimanje` | 24 h prije preuzimanja |
| `VoziloVraceno` | vozilo vraćeno, sa obračunom depozita |

`VoziloVraceno` je dodan u ovoj fazi. Tip je postojao u enumu i u seed podacima, ali ga
nijedan stvarni događaj nije stvarao — obavještenje o zatvorenom najmu i obračunu
depozita jednostavno nije postojalo. Sada `PrimopredajaService` poslije evidentiranog
povrata objavi `vozilo.vraceno`, a worker u toj poruci klijentu pošalje koliko je
depozita zadržano i koliko mu se vraća. Iznos se čita iz zapisa o povratu novca koji je
tada nastao, ne računa se ponovo — obračun je već urađen jednom, u servisu.

`RezervacijaPotvrdjena` ostaje samo u seed podacima: potvrda rezervacije i uspješno
plaćanje su isti trenutak, pa bi klijent na jedan događaj dobio dva obavještenja.

---

## Recenzije i obavijesti

> 🟢 Urađeno nakon faze 16, kao priprema za desktop i mobilnu aplikaciju.

### Recenzije

Autor i vozilo se **izvode iz rezervacije**, ne iz zahtjeva. Zahtjev nosi samo
`rezervacijaId`, ocjenu i komentar; da klijent šalje i vozilo, mogao bi ocijeniti vozilo
koje nije vozio, a da šalje autora, mogao bi recenziju potpisati tuđim imenom.

Prije upisa se provjerava troje:

| Provjera | Zašto |
|---|---|
| rezervacija pripada onome ko piše | inače se ocjenjuje tuđi najam |
| status je `Completed` | vozilo koje još nije vraćeno nema se šta ocijeniti |
| za tu rezervaciju još nema recenzije | inače bi jedan najam pomjerao prosječnu ocjenu više puta |

Treće je osigurano i jedinstvenim indeksom na `(KorisnikId, RezervacijaId)`, pa dvije
istovremene forme ne mogu proći.

**Izmjena je dozvoljena samo autoru i samo dok recenzija nije skrivena.** Drugi uslov nije
sitnica: bez njega bi klijent čija je recenzija sklonjena zbog sadržaja mogao promijeniti
tekst i vratiti je u opticaj, a da to niko iz agencije ne vidi.

**Brisanja nema.** `DELETE /api/recenzije/{id}` izvršava se kao skrivanje — zapis ostaje
u bazi, prosječna ocjena ostaje sljediva, a recenzija nestaje iz javnog prikaza,
prosjeka i sistema preporuke. Klijentska aplikacija ima dugme „obriši" kao i svugdje, pa
je bolje da ono radi ispravnu stvar nego da vraća grešku.

Klijent u listi vidi neskrivene recenzije i **svoju** skrivenu — da zna šta je napisao i
da je sklonjena. Osoblje vidi sve.

`GET /api/recenzije/za-ocjenjivanje` vraća završene najmove prijavljenog korisnika koji
još nisu ocijenjeni, jednim upitom sa provjerom nepostojanja recenzije. Mobilna
aplikacija po tome zna kada uopšte ponuditi ocjenjivanje.

### Obavijesti

Piše ih administrator, čitaju svi klijenti. Klijentu se prikazuju **samo aktivne
obavijesti čiji je datum objave prošao** — datum u budućnosti je zakazana objava. Obje
provjere su na serveru, i u listi i na detalju; neobjavljena obavijest klijentu vraća 404,
namjerno, da odgovor ne oda da nešto postoji i čeka objavu.

Slika se snima na disk kroz istu pohranu kao i fotografije vozila, sa validacijom po
magic bytes. U bazu ide samo putanja, a odgovor nosi URL slike i URL thumbnaila. Stara
slika se briše tek kad je nova upisana u bazu — obrnutim redoslijedom bi pad upisa
ostavio obavijest sa putanjom do fajla kojeg više nema. Brisanje obavijesti briše i njen
fajl, da na disku ne ostane slika na koju ništa ne pokazuje.

---

## Nalozi, uloge i profil

> 🟢 Urađeno nakon faze 16, kao priprema za desktop i mobilnu aplikaciju.

Dva odvojena puta do istog entiteta, sa različitim pravilima:

| Ruta | Ko | Kako se bira zapis |
|---|---|---|
| `/api/korisnici` | isključivo administrator | po identifikatoru, nad tuđim nalozima |
| `/api/profil` | svaki prijavljeni korisnik | **nema identifikatora** — vlasnik se čita iz tokena |

Na `/api/profil` zato i nema provjere vlasništva: tuđi profil se ne može ni adresirati,
pa nema šta da se provjerava. To je isti obrazac koji već koriste vozačke dozvole.

### Lozinka nikad nije dio forme za izmjenu

Uputstvo traži da forma za izmjenu korisnika ne prikazuje polja za lozinku, nego da
postoji zasebna radnja. Zato ni `KorisnikUpdateRequest` ni `ProfilUpdateRequest` nemaju
ta polja, a postoje dvije odvojene radnje:

| Radnja | Traži staru lozinku | Ko smije |
|---|---|---|
| `POST /api/auth/promjena-lozinke` | **da** | korisnik nad svojom |
| `POST /api/korisnici/{id}/reset-lozinke` | ne | administrator nad tuđom |

Administrator staru lozinku ne zna i ne treba je znati — zato je ta ruta administratorska
i samo administratorska.

### Šta se ne može ni sa administratorskim pravima

| Radnja | Zašto je odbijena |
|---|---|
| ukloniti sebi administratorsku ulogu | ostao bi bez prava da tu grešku ispravi, a moguće je i da je jedini administrator |
| deaktivirati vlastiti nalog | zaključao bi se van sistema |
| blokirati uposlenika ili administratora | blokada se odnosi na klijente; nalog osoblja se deaktivira |

### Brisanja nema

`DELETE /api/korisnici/{id}` izvršava se kao **deaktivacija**. Rezervacije, plaćanja i
recenzije korisnika moraju ostati — bez njih izvještaji o prihodu i iskorištenosti flote
govore neistinu. Deaktiviran nalog se ne može prijaviti, a historija ostaje čitava.

Blokada je nešto drugo od deaktivacije: blokiran klijent se i dalje može prijaviti i
vidjeti svoje rezervacije, ali ne može napraviti novu. To pravilo provodi
`RezervacijaService.ProvjeriKlijentaAsync` pri kreiranju, ne interfejs.

### Uloge

Uloge se ne unose kroz aplikaciju — fiksne su i dolaze iz seed podataka.
`GET /api/korisnici/uloge` ih vraća za padajuću listu, a `PUT /api/korisnici/{id}/uloge`
prima **cijelu novu listu**, ne dodavanje jedne po jedne. Tako je stanje poslije zahtjeva
uvijek tačno ono što je poslano, bez obzira šta je bilo prije.

Registracija kroz mobilnu aplikaciju i dalje nema nijedno polje za ulogu —
`RegisterRequest` je netaknut i uvijek dodjeljuje `Klijent`. Uloge se biraju samo na
administratorskom unosu, gdje ih bira administrator, a ne onaj ko nalog dobija.

---

## Pregled poslovanja i kalendar flote

> 🟢 Urađeno nakon faze 16, kao priprema za desktop aplikaciju.

### Pregled poslovanja

`GET /api/pregled-poslovanja` vraća sve što početni ekran prikazuje, u jednom odgovoru:
četiri metrike, raspored za danas, te presjek flote po tipu vozila i po poslovnici.

Sve vrijednosti su **gotovi agregati**. Računaju se na bazi grupisanim upitima i stižu
izračunate — Flutter ih samo prikazuje. Da se računaju u aplikaciji, morala bi povući sve
rezervacije i sva plaćanja da bi ispisala četiri broja.

Dvije definicije koje je bolje znati unaprijed:

**Trenutna iskorištenost** je udio flote koji je u ovom trenutku kod klijenata, a ne
mjesečna iskorištenost. Kartica odgovara na pitanje „koliko nam vozila sada radi".
Iskorištenost kroz period je nešto drugo — broj dana izdato — i računa se u izvještaju o
floti.

**Prihod** se računa iz `Placanje.NaplaceniIznos`, dakle iz stvarno naplaćenog, a ne iz
iznosa rezervacije. Rezervacija nosi koliko je trebalo naplatiti, a plaćanje koliko
jeste. Od toga se oduzimaju povrati koji nisu odbijeni ni poništeni, pa je neto prihod
broj koji agenciju stvarno zanima.

„Čeka obradu" je zbir neverifikovanih dozvola i neplaćenih rezervacija koje još drže
termin, i **razložen je** na te dvije stavke — da uposlenik odmah zna gdje da klikne
umjesto da traži. Rezervacije kojima je držanje isteklo se ne broje: njih preuzima
periodični posao u workeru i osoblje s njima nema šta raditi.

**Poređenje sa prošlim mjesecom** stiže u `metrike.poredba`. Kartice na početnom ekranu
uz vrijednost prikazuju i strelicu sa postotkom, pa se taj postotak računa na serveru.

Poredi se **isti raspon dana**, ne cijeli prošli mjesec: tekući mjesec do sada naspram
istog broja dana prošlog mjeseca. Da se poredi cijeli prošli mjesec, devetnaestog u
mjesecu bi svaka kartica pokazivala pad — ne zato što poslovanje ide lošije nego zato
što mjesec još nije završen. Kad je prošli mjesec kraći, kraj poredbenog perioda se
odsijeca na početak tekućeg, da periodi nikad ne bi zagazili jedan u drugi.

Porede se samo veličine koje se kroz period akumuliraju: neto prihod, nove rezervacije
(po datumu kreiranja) i završene rezervacije (po datumu vraćanja). Iskorištenost i „čeka
obradu" su trenutna stanja, ne zbirovi, pa za njih poređenje sa prošlim mjesecom nema
značenje i namjerno ih nema. `promjenaPosto` je prazno kad je prošla vrijednost nula —
rast sa nule nije „beskonačno posto" nego podatak koji se postotkom ne može izraziti, i
aplikacija u tom slučaju ne crta strelicu.

### Kalendar flote

`GET /api/kalendar-flote` vraća jedan red po vozilu sa blokovima zauzeća. Bez zadatog
perioda vraća tekuću sedmicu, od ponedjeljka; period je ograničen na 62 dana.

Blokovi se grade iz **istih zapisa** koje provjera dostupnosti smatra zauzećem —
potvrđene rezervacije, one koje čekaju plaćanje a još drže termin, i blokade vozila. Zato
prazan prostor u kalendaru znači da je vozilo stvarno slobodno, a ne da neko nije upisao
podatak. Uz njih se crtaju i završene rezervacije, jer se kalendar gleda i unazad: da se
ne crtaju, prošla sedmica bi izgledala kao da flota nije radila. Otkazanih nema — one
nikad nisu ni držale termin.

Odgovor nosi i `bufferSati`. Blok pokazuje stvarni termin najma, a buffer je podatak
kojim interfejs može nacrtati razmak i objasniti zašto termin odmah uz tuđi najam nije
slobodan. Deaktivirana vozila se ne prikazuju — njihov prazan red ne bi značio
„slobodno" nego „ne postoji".

### Kvar vozila iz kalendara

Klik na slobodan dan u kalendaru otvara ručni unos, a na dnu te forme stoji i prijava
kvara — isti potez, isto vozilo i isti termin, pa nema razloga tjerati uposlenika na
drugi ekran.

Blokada se ponaša drukčije nego što se na prvu očekuje: server je **ne odbija** ni kad
preklapa postojeću rezervaciju. To je namjerno, jer se vozilo pokvari bez obzira na to
što je iznajmljeno; kad bi sistem to odbijao, uposlenik ne bi imao način da zabilježi
stvarno stanje flote. Zato forma prije potvrde poziva
`GET /api/dostupnost/pogodjene-rezervacije` i prikazuje svaku pogođenu rezervaciju sa
**kontaktom klijenta** — blokada nikoga ne obavještava sama, pa uposlenik mora znati
koga zove.

Uz to stoji kvadratić „Skrati blokadu do prve rezervacije", uključen po defaultu: kvar
se tada evidentira od sada do početka prve rezervacije, a šta će sa tom rezervacijom —
otkazivanje, drugo vozilo, dogovor — ostaje odluka koju uposlenik donosi telefonom.
Forma tu odluku ne donosi umjesto njega, ali mu ne dozvoljava ni da je previdi.

### Ručni unos rezervacije

`POST /api/rezervacije/klijent/{klijentId}`, samo za osoblje.

Ovo je jedini put na kojem korisnik za kojeg se rezerviše dolazi izvana, i to nije
izuzetak od pravila da se identitet čita iz tokena: iz tokena se i dalje čita **ko
unosi**, i to mora biti osoblje. Klijent je ovdje podatak o kome se radi, isto kao vozilo
ili termin — a ne tvrdnja o tome ko poziva. Zato je u ruti, gdje je vidljiv, a ne u
tijelu zahtjeva.

Sve provjere su iste kao kod klijentskog unosa: kategorija i rok važenja dozvole,
dostupnost pod zaključanim vozilom, zalihe opreme, obračun cijene na serveru. Rezervacija
nastaje kao `Pending` i drži termin — status se ne može preskočiti samo zato što unos
dolazi sa šaltera. Dodatno se provjerava da je ciljni korisnik zaista klijent, da se
rezervacija ne zavede na nalog uposlenika.

**Odabir klijenta.** Da bi uposlenik unio rezervaciju, mora pronaći klijenta — a
`/api/korisnici` je administratorski. Umjesto da se cijela lista naloga otvori i
uposleniku, postoji zasebna ruta `GET /api/korisnici/klijenti?tekst=` u vlastitom
kontroleru (`KlijentController`). Vlastiti kontroler nije stvar ukusa: ASP.NET Core
atribute `[Authorize]` sa klase i metode sabira, pa metoda u administratorskom
kontroleru ne može pustiti uposlenika ma šta na njoj pisalo. Test
`PravilaAutorizacijeTests` prolazi kroz sve kontrolere i pada ako neka metoda navede
ulogu koju njena klasa odbija. Ruta vraća samo aktivne naloge s ulogom klijenta i samo
polja potrebna za odabir: ime, prezime, email, telefon, je li blokiran i stanje vozačke
dozvole. Nalozi osoblja se tu ne pojavljuju ni pri pretrazi po imenu, a uloge se ne
vraćaju. Stanje dozvole i blokada su tu da uposlenik unaprijed vidi hoće li rezervacija
proći — server je bez odobrene dozvole ionako odbija, ali je bolje da se to vidi prije
nego se popuni cijela forma.

---

## PDF izvještaji

> 🟢 Faza 19 je gotova: iskorištenost flote i finansijski pregled.

Oba izvještaja imaju **dva oblika**: podatak i PDF. Podatak (`GET .../iskoristenost-flote`)
služi za pregled prije generisanja — uputstvo traži da korisnik provjeri parametre prije
nego dobije dokument — a PDF se gradi iz tog **istog** podatka, pa pregled i dokument ne
mogu pokazivati različite brojeve.

PDF se generiše na serveru kroz QuestPDF i vraća kao bajtovi; desktop ih preuzima i
otvara za pregled i štampu. Da se generiše u Flutteru, logika izvještaja bi postojala na
dva mjesta, a agregacije bi se računale u aplikaciji umjesto na bazi.

> QuestPDF traži da se licenca navede prije prvog generisanja. Postavlja se jednom u
> `Program.cs`; bez toga prvi poziv baca izuzetak, i to tek u trenutku kad neko zatraži
> dokument.

### Iskorištenost flote

Jedan red po vozilu: tip, poslovnica, broj najmova, dana izdato, iskorištenost, prihod i
prosječna ocjena — uz zbirni red i sumarni prikaz po tipu vozila.

**Dani izdato se računaju iz preklapanja najma sa periodom, a ne iz trajanja najma.**
Preklapanje računa baza: početak je kasniji od dva datuma, kraj raniji. Najam koji je
počeo prije perioda ili se završava poslije njega ulazi samo onim dijelom koji u period
stvarno pada. Računa se u satima pa dijeli sa 24, jer bi brojanje kalendarskih dana
dvosatni najam preko ponoći prikazalo kao cijeli dan.

U izvještaju su i vozila **bez ijednog najma** — vozilo koje cijeli mjesec nije izdato je
najvažniji red u ovom izvještaju, jer košta a ne zarađuje. Ne ulaze, međutim, vozila koja
su povučena iz ponude i u periodu nisu radila: ona imaju nula dana izdato, a ulazila bi u
nazivnik i spuštala iskorištenost cijele flote. Vozilo koje je radilo pa je povučeno
ostaje, jer je zaradilo prihod koji bi inače nestao iz izvještaja.

Prihod dolazi iz stvarno naplaćenih iznosa, ne iz ugovorene vrijednosti rezervacije.
Ocjene se ne ograničavaju na period: ocjena govori o vozilu, ne o mjesecu, pa bi suđenje
po tromjesečnom isječku dalo prosjek iz dvije-tri recenzije. Prosjek u zbirnom redu se
računa preko broja ocjena, ne preko vozila — inače bi vozilo sa jednom ocjenom vuklo
prosjek flote jednako kao ono sa četrdeset.

### Finansijski pregled

Jedan red po mjesecu i poslovnici: broj rezervacija, naplaćeno, refundirano, neto prihod
i prosječna vrijednost najma.

Sve brojke dolaze iz **plaćanja**, ne iz iznosa rezervacija. Rezervacija nosi koliko je
trebalo naplatiti, a plaćanje koliko jeste. Broj rezervacija je broj **različitih**
rezervacija koje su u tom mjesecu plaćene, ne broj plaćanja — jedna rezervacija može
imati više pokušaja naplate, a izvještaj broji najmove. Povrat koji je odbijen ili
poništen se ne broji: to nije novac koji je otišao.

### Zašto se prihod u dva izvještaja razlikuje

Isti period zna dati različit iznos u jednom i drugom izvještaju, i to nije greška nego
posljedica toga šta svaki od njih broji.

| Izvještaj | Šta je „u periodu" |
|---|---|
| iskorištenost flote | najam čiji se **termin** preklapa sa periodom |
| finansijski pregled | plaćanje koje je u periodu **nastalo** |

Klijent koji je u martu platio najam za juni ulazi u finansijski pregled za mart, a u
izvještaj o floti za juni. Svaki od dva izvještaja odgovara na svoje pitanje: prvi koliko
je flota radila, drugi koliko je novca ušlo. Uz to je prihod u izvještaju o floti bruto —
povrati se prate u finansijskom pregledu, gdje im je i mjesto.

### Agregacije

Nijedan izvještaj ne učitava zapise pa ih broji u memoriji. Iskorištenost flote koristi
četiri grupisana upita — vozila, najmovi, prihodi i ocjene — koji se zatim spajaju po
identifikatoru vozila. Četiri upita a ne jedan zato što jedan upit sa tri ugniježdena
agregata SQL Server rješava troškovnije nego tri odvojena; ono što se izbjegava je upit
po vozilu, a ne postojanje više upita.

Period je ograničen na dvije godine, a bez zadatog perioda izvještaj pokriva posljednjih
365 dana.

---

## Kome se vjeruje: klijent naspram servera

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

### Plaćanje

| Podatak | Odakle | Napomena |
|---|---|---|
| identifikator rezervacije | klijent (ruta) | provjerava se da je rezervacija njegova |
| iznos | **server** | `Rezervacija.UkupanIznos`, upisan pri kreiranju |
| valuta | **server** | EUR |
| idempotency ključ | **server** | iz rezervacije i rednog broja pokušaja |
| „plaćanje je uspjelo" | **Stripe** | server pita Stripe; poruka aplikacije je samo signal da pita |
| `NaplaceniIznos` | **Stripe** | `AmountReceived` iz odgovora, i mora biti jednak očekivanom |
| `IsPaid`, `Status` | **server** | isključivo kroz `PrimijeniStanjeAsync` i state machine |
| iznos povrata | **server** | iz `NaplaceniIznos`, nikad iz cjenovnika |
| webhook događaj | **Stripe**, uz potpis | sadržaj služi samo da se nađe plaćanje; stanje se ponovo čita iz Stripe-a |

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

> Kreiranje rezervacije i potvrda plaćanja su ovdje; izdavanje i povrat vozila su u sekciji o primopredaji.

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

1. Plaćanje postoji → 404
2. Klijent potvrđuje samo svoje (osoblje smije svako) → 403
3. Već je `Succeeded` → 200 sa istim stanjem, **bez efekata** — prije ijednog upisa
4. Transakcija i zaključavanje reda rezervacije
5. Ponovo: već je `Succeeded`? (webhook je mogao stići dok se čekalo) → 200
6. Stripe kaže da intent ne postoji → plaćanje `Canceled`, 400
7. Status intenta nije `succeeded` → status se upiše, 400 sa porukom po stanju
   (odbijena kartica, 3D Secure, obrada u toku)
8. `AmountReceived` ≠ očekivani iznos u centima → 400, rezervacija se ne potvrđuje
9. Rezervacija već ima uspješno plaćanje → povrat cijelog iznosa
10. Rezervacija više ne čeka plaćanje → povrat cijelog iznosa
11. Držanje isteklo → zaključavanje vozila i ponovna provjera dostupnosti; zauzeto →
    otkazivanje uz pun povrat
12. `Pending → Confirmed`, `IsPaid = true`, `DrziDo` prazno
13. Commit, pa tek onda slanje povrata i poništavanje ostalih otvorenih intenta

---

## Šta se dešava kad dva zahtjeva stignu istovremeno

> ⬜ Popunjava se u fazama 9, 11 i 12.

Ovdje se za svaki scenarij mora znati **koji konkretno mehanizam** ga štiti: provjera
prije upisa, jedinstveno ograničenje u bazi, eksplicitna transakcija, concurrency
token, zaključavanje reda ili uslovni upis. Gdje mehanizma nema, to i piše.

| Scenarij | Čime je riješeno | |
|---|---|---|
| Dva zahtjeva za isto vozilo i preklapajući termin | transakcija i `UPDLOCK, HOLDLOCK` na redu vozila prije provjere dostupnosti | ✅ testirano u fazi 11 |
| Dvostruko slanje iste forme | jedinstveni indeks `(KorisnikId, VoziloId, DatumOd)`; `SacuvajAsync` kršenje pretvara u 400 | ✅ |
| Dva zahtjeva za payment intent iste rezervacije | zaključavanje reda rezervacije; drugi zahtjev čeka i dobija isti intent | ✅ |
| Dva uspješna plaćanja iste rezervacije | zaključavanje, provjera prije upisa, i filtrirani jedinstveni indeks gdje `Status = 3`; drugi novac se vraća | ✅ |
| Otkazivanje i potvrda plaćanja istovremeno | oba puta zaključavaju red rezervacije; ko dođe drugi vidi novo stanje, a novac za otkazanu rezervaciju se vraća | ✅ |
| Potvrda iz aplikacije i webhook istovremeno | isto zaključavanje, pa ponovna provjera `Succeeded` poslije njega | ✅ |
| Isti Stripe događaj dvaput | provjera prije obrade i jedinstveni indeks na `ProviderEventId` | ✅ |
| Dvije recenzije istog najma | jedinstveni indeks `(KorisnikId, RezervacijaId)` | 🟡 indeks postoji |
| Dvije primopredaje istog tipa | zaključavanje reda rezervacije, provjera prije upisa i jedinstveni indeks `(RezervacijaId, Tip)` | ✅ |
| Automatsko otkazivanje i plaćanje istovremeno | posao u workeru zaključava red rezervacije i **poslije toga ponovo pročita** status i `DrziDo`; ako je plaćeno, odustaje | ✅ |

Razlog za 🟡 umjesto ✅ vrijedi razumjeti. Jedinstveni indeks stvarno **sprječava**
duplikat i onda kad provjera u kodu ne uhvati trku — baza jednostavno odbije drugi
upis. Ali odbije ga kroz `DbUpdateException`, koji, ako ga servis ne uhvati, do
klijenta stigne kao 500 sa nerazumljivom porukom.

Dakle: podatak je zaštićen, korisničko iskustvo nije. Dok servis to ne obradi i ne
vrati razumnu poruku, pošteno je reći da stvar nije završena.

---

## Idempotentnost

> ✅ Faza 12.

| Operacija | Kako | |
|---|---|---|
| `POST /api/placanja/{id}/confirm` | ako je plaćanje već `Succeeded`, vraća 200 i **ne ponavlja efekte** — ni status, ni audit zapis, ni (od faze 14) email | ✅ |
| Kreiranje payment intenta | ako postoji otvoren `Created`/`Pending` intent koji Stripe još vodi, vraća se on | ✅ |
| Stripe webhook | `ObradjeniWebhookEvent.ProviderEventId` je jedinstven; postojanje zapisa znači da je događaj već obrađen | ✅ |
| Idempotency ključ prema Stripe-u | `rez-{id}-v{n}-{ticks}` za intent, `povrat-{id}-{ticks}` za povrat | ✅ |
| Ponovno slanje povrata | isti zapis, isti ključ — Stripe vraća prethodni rezultat umjesto novog povrata; periodični posao u workeru koristi isti put | ✅ |
| Podsjetnik za preuzimanje | šalje se samo ako za tu rezervaciju ne postoji `Notifikacija` tipa `PodsjetnikPreuzimanje` | ✅ |
| Kod za reset lozinke | novi zahtjev poništava sve ranije kodove; iskorišten kod se ne može upotrijebiti drugi put | ✅ |

---

## Životni ciklus rezervacije

> ✅ Implementirano u fazi 11.

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

> ✅ Pravilo i obračun su iz faze 11c, a izvršavanje prema Stripe-u iz faze 12 —
> vidi sekciju o plaćanju.

| Kad se otkazuje | Povrat najma |
|---|---|
| više od 7 dana prije preuzimanja | 100 % |
| od 3 do 7 dana prije | 50 % |
| manje od 3 dana prije | 0 % |
| agencija otkazuje | 100 %, uvijek |

Depozit se vraća **uvijek**, neovisno o politici — nije naknada nego polog.

Iznos se računa iz `Placanje.NaplaceniIznos` — stvarno naplaćenog iznosa koji je
vratio Stripe — a nikad ponovnim obračunom iz cjenovnika. Razlika je bitna: kad se
tarifa promijeni u međuvremenu, povrat za staru rezervaciju mora ostati isti.

`GET /api/rezervacije/{id}/obracun-otkazivanja` vraća izračunati iznos **bez
izvršavanja akcije**, da klijent vidi tačan iznos prije nego potvrdi. Taj odgovor je
prikaz, a ne obećanje — pri samom otkazivanju se računa iznova.

Detaljno obrazloženje je u sekciji o otkazivanju, uz fazu 11.

---

## Sistem preporuke

> 🟢 Faza 16 je gotova. Potpun opis je u `recommender-dokumentacija.md`; ovdje stoji
> samo sažetak i razlog zašto sistem ima dva puta.

Sistem ima **ML model** i **rezervni put**, i to nisu iste vrste stvari.

**ML model — matrična faktorizacija.** `MatrixFactorizationTrainer` iz ML.NET-a (LIBMF)
uči latentne faktore iz matrice korisnik × model vozila. Ulaz su ocjene iz `Recenzija`
i završeni najmovi bez recenzije, koji ulaze sa prosjekom flote. Model predviđa ocjenu
koju bi korisnik dao vozilu koje još nije vozio, a predikcija se svodi na skor 0–1.
Mjeri se RMSE-om na 20% stvarnih ocjena izdvojenih iz učenja; procijenjene vrijednosti
nikad ne ulaze u test skup.

Stavka je **model vozila**, ne pojedinačno vozilo: flota ima 8–10 modela i 30–40 vozila,
pa je matrica na nivou modela popunjena oko 30% umjesto 8%. U odgovoru se zato zadržava
jedan primjerak po modelu.

Broj latentnih faktora i jačina regularizacije se ne upisuju u kod nego se biraju
pretragom po mreži, a svaki kandidat se ocjenjuje petostrukom unakrsnom provjerom nad
podacima za učenje. Skup za provjeru se izdvaja prvi i do kraja se ne dira, pa se model
ne mjeri na podacima prema kojima je i podešen.

Seed ocjene su izmijenjene u istoj fazi: svaki demo klijent ima tip vozila koji mu leži i
ocjene to prate. Dok su bile čisto nasumične, u podacima nije postojao obrazac koji bi
bilo koji model mogao naučiti, pa je evaluacija davala negativan R² — model lošiji od
pogađanja prosjeka.

**Rezervni put — ponderisano bodovanje.** `skor = 0,6 × sličnost + 0,4 × popularnost`,
sa težinama tip 0,35 · cijena 0,25 · lokacija 0,20 · marka 0,10 · kubikaža 0,10.
Koristi se kad korisnik nije bio u podacima za učenje ili kad model nije treniran zbog
premalo podataka. **To nije mašinsko učenje** i nigdje se tako ne predstavlja — u
odgovoru nosi oznaku `RezervnaHeuristika` i prazno polje `predvidjenaOcjena`.

Oba puta filtriraju kandidate kroz **iste servise** koje koriste pretraga i kreiranje
rezervacije (`IAvailabilityService`, `IDozvolaService`). Da preporuke imaju vlastitu
provjeru, klijentu bi se moglo ponuditi vozilo koje mu rezervacija odbija.

Zapis u `HistorijaPretrage` upisuje `HistorijaPretrageService`, pozvan iz
`VoziloService.GetAsync`. Ne bilježe se pretrage osoblja, zahtjevi bez ijednog filtera i
druga i dalje stranica istog rezultata.

`GET /api/preporuke/model` vraća stanje modela: na čemu je učen, koliko korisnika i
modela vozila poznaje, te RMSE, MAE i R². `POST /api/preporuke/model/treniraj` trenira
na zahtjev.

Nad seed podacima model daje RMSE 0,711 naspram 0,981 koliko daje pogađanje prosjeka,
dakle R² 0,475. Greška pri odabiru parametara (0,707) i greška pri provjeri (0,711) su
praktično iste, što znači da model nije naučio podatke napamet.

> **Zašto dva puta.** Mentor je na prijavu teme odgovorio da content-based nije naziv
> algoritma nego pristup i da je bodovanje sa unaprijed zadatim težinama heuristika, a
> ne ML model, te tražio stvarnu ML metodu. Matrična faktorizacija je uvedena kao
> odgovor na to. Ranije bodovanje nije obrisano nego svedeno na ono što jedino i može
> biti — rješenje za hladni start.

> Uputstvo (sekcija 2.4) izričito kaže da svaki signal naveden u dokumentaciji mora
> biti stvarno korišten u kodu. Ako se algoritam ikad promijeni, `recommender-dokumentacija.md`
> se mijenja u istom commitu.

---

## Sigurnost

| Pravilo | |
|---|---|
| `[Authorize]` na svim kontrolerima, `[AllowAnonymous]` samo u `AuthController`-u (`login`, `register` i reset zaboravljene lozinke) | 🟡 vidi sekciju o resetu |
| Kod za reset i lozinke hashirani BCrypt-om; kod se generiše `RandomNumberGenerator`-om | ✅ |
| `userId` uvijek iz JWT tokena kroz `IHttpContextAccessor` | ✅ |
| `RegisterRequest` bez polja `Role` i `IsAdmin` | ✅ |
| Odjava invalidira token na serveru — `OpozvaniToken` plus middleware | ✅ |
| Upload i download provjeravaju vlasništvo nad resursom | ✅ |
| MIME tip se validira po magic bytes, ne po ekstenziji | ✅ |
| Lozinke kroz BCrypt | ✅ |
| Kodovi i tokeni kroz `RandomNumberGenerator`, nikad `System.Random` | ✅ |
| Stripe webhook zaštićen potpisom kroz vlastitu shemu, bez `[AllowAnonymous]` | ✅ |
| Samo testni Stripe ključ; live ključ obara pokretanje | ✅ |
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
