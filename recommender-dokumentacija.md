# Sistem preporuke — SunnyRides

**Seminarski rad iz predmeta Razvoj softvera II · Ammar Puce, IB220182**

Ovaj dokument opisuje sistem preporuke onako kako je **stvarno implementiran**. Svaka
težina, granica i formula koja se ovdje spominje postoji kao konstanta u kodu, a svaki
signal koji se ovdje navodi kod zaista i čita. Signal koji bi stajao u dokumentu a ne bi
se koristio u kodu učinio bi ovaj dokument netačnim opisom sistema, pa ga nema.

---

## 1. Šta je ovdje mašinsko učenje, a šta nije

Sistem ima **dva puta** i oni nisu iste vrste. Razlika je važna i vidi se u samom
odgovoru API-ja, u polju `metoda`:

| Put | Šta je | Kada se koristi |
|---|---|---|
| **Matrična faktorizacija** | istreniran ML model; parametri se **uče** iz podataka | korisnik je bio u podacima za učenje |
| **Rezervna heuristika** | pravilo sa unaprijed zadatim težinama, **nije** ML | korisnik je nov ili model još nije treniran |

Prvi put je ono što predmet traži kao ML metodu. Drugi postoji zato što nijedan model
preporuke ne može reći ništa o korisniku o kojem nema nijedan podatak — to je poznat
problem hladnog starta i rješava se upravo ovako, pravilom koje ne traži historiju.

Rezervni put se nigdje ne predstavlja kao mašinsko učenje. U odgovoru nosi oznaku
`RezervnaHeuristika` i prazna polja `predvidjenaOcjena`, pa se iz same preporuke uvijek
zna odakle je došla.

Kod koji sve to čini:

| Fajl | Šta radi |
|---|---|
| `Preporuke/Ml/ModelPreporukeMf.cs` | treniranje, evaluacija i predikcija ML modela |
| `Preporuke/Ml/PripremaInterakcija.cs` | čist prevod baze u matricu za učenje |
| `Preporuke/BodovanjePreporuke.cs` | čist račun rezervne heuristike |
| `Preporuke/RecommenderService.cs` | filteri, izbor puta, sastavljanje odgovora |
| `Preporuke/HistorijaPretrageService.cs` | upis pretrage, ulaz za rezervni put |
| `SunnyRides.Tests/PripremaInterakcijaTests.cs` | testovi pripreme podataka |
| `SunnyRides.Tests/BodovanjePreporukeTests.cs` | testovi rezervnog računa |

---

# Dio 1 — ML model

## 2. Algoritam

**Matrična faktorizacija**, kroz `MatrixFactorizationTrainer` iz ML.NET-a (ispod je
LIBMF). To je standardni algoritam kolaborativnog filtriranja: iz rijetko popunjene
matrice *korisnik × stavka* uči se skup **latentnih faktora** za svakog korisnika i
svaku stavku, tako da njihov skalarni proizvod što bolje pogađa poznate ocjene. Ocjena
koju korisnik nije dao predviđa se istim proizvodom.

```
ocjena(korisnik, model) ≈ p(korisnik) · q(model)
```

Vektori `p` i `q` nisu zadati — dobijaju se minimizacijom greške nad poznatim ocjenama.
Tu je razlika u odnosu na heuristiku: težine niko nije upisao, model ih je naučio.

**Parametri učenja se biraju mjerenjem, ne pogađanjem**

Broj latentnih faktora i jačina regularizacije nisu upisani u kod kao odluka — traže se
pretragom po mreži, a svaki kandidat se ocjenjuje **petostrukom unakrsnom provjerom** nad
podacima za učenje:

| Parametar | Kandidati |
|---|---|
| broj latentnih faktora (rang) | 1 · 2 · 4 · 8 |
| jačina regularizacije (lambda) | 0,01 · 0,05 · 0,1 · 0,3 |
| broj iteracija | 100, fiksno |
| sjeme generatora | 0, fiksno |

Za svaki od 16 parova podaci se podijele na pet dijelova; model se pet puta nauči na
četiri i izmjeri na petom, pa se uzme prosječna greška. Bira se par sa najmanjim
prosjekom. To je 80 kratkih treniranja, što na ovoj količini podataka traje kraće od
jednog upita prema bazi.

**Zašto unakrsna provjera, a ne jedan izdvojeni skup.** Prva verzija je parametre birala
po jednom skupu od desetak redova. Greška izmjerena na deset redova više je stvar slučaja
nego mjera kvaliteta, pa je izbor ispao loš na vidljiv način: uzeti su rang 8 i lambda
0,5, model se sveo na jednu te istu vrijednost za sva vozila (sve predikcije između 3,0 i
3,4), a greška na skupu za provjeru bila je više nego dvostruko veća od one po kojoj su
parametri izabrani. Unakrsna provjera koristi sve podatke za učenje i taj problem nema.

Rang 8 nad dvanaest korisnika i deset modela vozila znači blizu dvjesta parametara koji
se uče iz pedesetak redova — zato su u mreži i rang 1 i rang 2, koji na ovoj količini
podataka najčešće i pobijede.

Kad podataka nema ni za unakrsnu provjeru, uzimaju se umjerene vrijednosti (rang 4,
lambda 0,1) — na malo podataka je to sigurniji izbor. Odabrani par se vidi u
`/api/preporuke/model`.

### Stavka je model vozila, ne pojedinačno vozilo

Flota ima 30–40 vozila, ali samo 8–10 modela, i po nekoliko primjeraka istog modela.
Matrica na nivou vozila bila bi popunjena oko 8%, a na nivou modela oko 30% — a
faktorizacija na prerijetkoj matrici ne nauči ništa upotrebljivo.

Osim toga, korisniku je svejedno koji primjerak dobija. Zato model predviđa ocjenu za
**model vozila**, a sistem zatim ponudi konkretan slobodan primjerak. Iz istog razloga
se u odgovoru zadržava **jedan primjerak po modelu** — bez toga bi prve tri preporuke
lako bile isti skuter tri puta, sa različitim registracijama.

---

## 3. Podaci za učenje

Matrica se gradi iz dva izvora:

| Izvor | Vrijednost u matrici | Oznaka |
|---|---|---|
| `Recenzija` (nije skrivena) | ocjena 1–5 kako ju je korisnik dao | stvarna ocjena |
| `Rezervacija` u statusu `Completed`, bez recenzije | prosječna ocjena flote | procijenjena |

Isti par korisnik–model može imati više ocjena (više najmova istog modela); tada ulazi
**prosjek** tih ocjena, jer matrica ima jedno polje po paru.

**Zašto se najam bez recenzije uopšte koristi.** Recenzija ima manje nego najmova, a
matrična faktorizacija živi od popunjenosti matrice. Najam bez recenzije je poštena
izjava „uzeo je to vozilo i nije se žalio" — ni oduševljenje ni nezadovoljstvo, nego
prosječno iskustvo. Zato dobija prosjek flote, vrijednost izračunatu iz podataka, a ne
broj koji je neko odabrao.

**Zašto to ne kvari evaluaciju.** Procijenjeni redovi nose oznaku da nisu stvarna
ocjena i **nikad ne završe u test skupu**. Da završe, model bi se mjerio prema broju
koji smo mu sami zadali, pa bi rezultat izgledao bolje nego što jeste.

**Skrivene recenzije ne ulaze u učenje**, isto kao što ne ulaze ni u prosječnu ocjenu.
Model ne smije učiti iz onoga što je moderacija uklonila.

### Prag ispod kojeg se ne trenira

| Uslov | Vrijednost |
|---|---|
| najmanje stvarnih ocjena | 15 |
| najmanje različitih korisnika | 3 |

Ispod toga se model **ne trenira** i sve ide rezervnim putem, uz jasnu napomenu u
`/api/preporuke/model`. Faktorizacija nad šačicom redova nauči šum, a predikcija koja se
ni na šta ne oslanja gora je od poštenog pada na rezervno pravilo.

---

## 4. Evaluacija

Greška se mjeri **ugniježdenom unakrsnom provjerom**.

Podaci se dijele na pet dijelova. Svaki dio jednom bude skup za provjeru, a nad preostala
četiri se — još jednom unakrsnom provjerom, na tri dijela — biraju parametri, nauči model
i predvide ocjene za taj dio. Predikcije iz svih pet prolaza se spoje i mjere zajedno.

```
        ┌─ dio 1 ─┬─ dio 2 ─┬─ dio 3 ─┬─ dio 4 ─┬─ dio 5 ─┐
prolaz 1│ PROVJERA│  učenje │  učenje │  učenje │  učenje │  ← parametri se biraju
prolaz 2│  učenje │ PROVJERA│  učenje │  učenje │  učenje │    unutar sivog dijela,
prolaz 3│  učenje │  učenje │ PROVJERA│  učenje │  učenje │    bez ijednog pogleda
prolaz 4│  učenje │  učenje │  učenje │ PROVJERA│  učenje │    u dio koji se mjeri
prolaz 5│  učenje │  učenje │  učenje │  učenje │ PROVJERA│
        └─────────┴─────────┴─────────┴─────────┴─────────┘
```

**Zašto ne jedan izdvojeni skup.** Uz sedamdesetak ocjena jedan izdvojeni skup ima
trinaestak redova, a greška izmjerena na trinaest redova nije mjera kvaliteta nego stvar
slučaja — zna ispasti dvostruko veća ili manja od stvarne. Ovako svaka ocjena tačno
jednom posluži za mjerenje, pa je rezultat izračunat nad svima.

Predikcije iz svih prolaza se spajaju i mjere zajedno, a ne prosječuju po prolazima:
prosjek R² po malim skupovima nije isto što i R² nad svim mjerenjima.

Ovim se mjeri **postupak**, ne jedan model — u svakom prolazu se parametri biraju iznova.
Konačni model koji sistem koristi uči na svim podacima, jer je svaki podatak vrijedan, a
koliko taj postupak griješi već je izmjereno.

Procijenjeni redovi (najam bez recenzije) uvijek ostaju u učenju i nikad ne završe u
skupu za provjeru: oni pomažu modelu da popuni matricu, ali nisu istina prema kojoj se
model smije mjeriti.

U odgovoru zato stoje dva broja: `rmse` je greška iz ugniježdene provjere, a `rmseOdabira`
prosječna greška najboljeg para parametara u unutrašnjoj provjeri. Prvi je mjera
kvaliteta, drugi samo trag kako su parametri izabrani.

Sjeme generatora je fiksno, pa je podjela uvijek ista; inače se ne bi moglo tvrditi da
je promjena rezultata posljedica izmjene modela a ne slučaja. Ako bi izdvajanje test
skupa spustilo skup za učenje ispod praga, test skup ostaje prazan — bolje ne izmjeriti
grešku nego učiti na premalo podataka da bi se ona izmjerila.

Računaju se **RMSE**, **MAE** i **R²**, i to u vlastitoj klasi `Mjere`, a ne kroz
`MLContext.Regression.Evaluate`. Dva su razloga: mjeri se ono što se stvarno servira
(predikcija se prije prikaza svodi na raspon 1–5, pa i greška mora biti računata nad
svedenom vrijednošću), i mjere se moraju moći izračunati nad spojenim predikcijama iz
svih prolaza unakrsne provjere.

RMSE je u istoj jedinici kao ocjena: vrijednost 0,8 znači da predikcija u prosjeku promaši
za nešto manje od jedne zvjezdice.

Rezultat se čita kroz `GET /api/preporuke/model` i tu se vidi i na čemu je model učen:

```json
{
  "treniran": true,
  "treniranUtc": "2026-09-18T10:59:32Z",
  "brojInterakcija": 74,
  "brojStvarnihOcjena": 74,
  "brojProcijenjenih": 0,
  "brojKorisnika": 12,
  "brojModelaVozila": 10,
  "brojIteracija": 100,
  "rang": 4,
  "lambda": 0.01,
  "rmseOdabira": 0.707,
  "rmse": 0.711,
  "mae": 0.514,
  "rKvadrat": 0.475,
  "rmseOsnovni": 0.981,
  "brojMjerenja": 74
}
```

> Ovo je stvarno izmjereno nad seed podacima, ne primjer. Vrijednosti se mijenjaju kako
> pristižu nove ocjene i čitaju se sa tog endpointa.

**Kako se ovaj rezultat čita.** Model griješi 0,711, a pogađanje prosjeka 0,981 — dakle
objašnjava oko 48% odstupanja u ocjenama (R² 0,475) i prosječno promašuje nešto preko pola
zvjezdice (MAE 0,514).

Najvažniji je odnos `rmseOdabira` (0,707) i `rmse` (0,711). Razmak između ta dva broja je
mjera učenja napamet: model koji je zapamtio skup za učenje ima nisku grešku pri odabiru i
visoku pri provjeri. Ovdje razmaka nema, pa rezultat nije slučajnost. U jednoj ranijoj
verziji taj odnos je bio 0,64 prema 1,48 i to je bio jasan znak da nešto ne valja.

**Kako se čita R².** Vrijednost 0 znači da model radi tačno kao da uvijek pogađa prosjek
svih ocjena; pozitivna znači da je bolji od toga, negativna da je lošiji. Zato se uz
grešku vraća i `rmseOsnovni` — greška tog najprostijeg predviđanja. RMSE od 0,84 sam po
sebi ne govori ništa dok se ne zna da pogađanje prosjeka daje 1,07.

**Koliko se uopšte može.** Ocjene nisu potpuno predvidive ni u principu: isti korisnik
isti tip vozila ponekad ocijeni peticom a ponekad četvorkom. Taj dio odstupanja nijedan
model ne može objasniti, pa postoji donja granica ispod koje se ne ide. Zato RMSE od 0,1
ne bi bio odličan rezultat nego znak da je nešto procurilo iz skupa za provjeru u učenje.

**Zašto historija ide skoro godinu i po unazad.** Sistem preporuke uči iz ocjena, a uz
kratku historiju svaki par korisnik–model vozila ima najviše jednu ocjenu — i ta jedna
nosi puno slučajno odstupanje. Sa više najmova isti par dobije više ocjena koje se
usrednje, pa šum pada i obrazac se vidi. Matrica je 13 korisnika × 10 modela, dakle 130
polja: na toliko malom prostoru je broj ocjena po polju važniji od broja korisnika.

**Zašto seed podaci imaju strukturu.** U ranijoj verziji su seed ocjene bile izvučene
nasumično, neovisno o tome ko ocjenjuje i šta ocjenjuje. U takvim podacima ne postoji
obrazac između korisnika i vozila, pa nijedan model preporuke ne može biti bolji od
pogađanja prosjeka — to se na evaluaciji vidjelo kao izrazito negativan R². Sada svaki
demo klijent ima tip vozila koji mu leži i ocjene to prate, sa namjerno ostavljenim
preklapanjem: i omiljeni tip ponekad razočara, a tuđi ponekad prijatno iznenadi. Stvarni
korisnici imaju ukus, pa ga demo podaci moraju imati — inače se ne testira model nego
šum.

---

## 5. Kada se trenira

Model je singleton u memoriji. Trenira se pri prvom pozivu preporuka i osvježava kad
istekne **6 sati**, jer treniranje na ovoj količini podataka traje sekundu-dvije, a
predikcija je hiljadu puta jeftinija. Nove ocjene do osvježavanja ne utiču na
predikciju — to je cijena toga što se ne trenira pri svakom zahtjevu.

`POST /api/preporuke/model/treniraj` (administrator) trenira odmah, bez čekanja.

Treniranje je zaštićeno semaforom, pa dva istovremena zahtjeva ne mogu pokrenuti dva
učenja. Predikcija ide kroz `ITransformer.Transform` nad cijelim skupom kandidata
odjednom, a ne kroz `PredictionEngine`, koji nije siguran za istovremeno korištenje iz
više niti.

---

## 6. Od predikcije do liste

1. Skupe se kandidati — vozila koja korisnik uopšte može rezervisati (tačka 9).
2. Model predvidi ocjenu za svaki **model vozila** među kandidatima.
3. Ocjena 1–5 se svede na skor 0–1: `skor = (ocjena − 1) / 4`.
4. Zadrži se jedan primjerak po modelu vozila.
5. Poredak po skoru, kod jednakih po identifikatoru — da dva uzastopna poziva ne vrate
   istu listu u drugom redoslijedu.

Model vozila o kojem u podacima za učenje nema traga dobija prosjek flote: trener za
nepoznat ključ vraća `NaN`, a neutralna vrijednost je poštenija od nasumične.

Obrazloženje uz stavku je ono što model zaista tvrdi:

> „Model procjenjuje da biste ovo vozilo ocijenili sa 4,3 od 5, prema ocjenama korisnika
> sličnog ukusa."

---

# Dio 2 — Rezervni put (hladni start)

## 7. Kada se koristi

Kad korisnik nije bio u podacima za učenje — nema nijednu recenziju ni završen najam —
ili kad model uopšte nije treniran zbog premalo podataka. Tada se preporuke računaju
pravilom opisanim ispod. **To nije mašinsko učenje** i tako je i označeno u odgovoru.

```
skor = 0,6 × sličnost + 0,4 × popularnost
```

## 8. Kako se računa

### Profil korisnika

| Izvor | Tabela | Težina |
|---|---|---|
| pretrage | `HistorijaPretrage`, zadnjih 200 zapisa | 1 |
| završeni najmovi | `Rezervacija` sa statusom `Completed` | 2 |

Brojači se pretvore u udjele koji se sabiraju u 1.

| Signal | Iz pretrage | Iz završenog najma |
|---|---|---|
| tip vozila | `TipVozilaId` | tip vozila iz modela |
| marka | `MarkaId` | marka iz modela |
| grad | `GradId`, ili grad poslovnice | grad poslovnice |
| cjenovni rang | sredina raspona `CijenaOd`–`CijenaDo` | dnevna tarifa vozila |
| kubikažni razred | — | kubikaža modela |

**Kubikaža dolazi samo iz završenih najmova**, jer zahtjev za pretragu nema polje za
kubikažu. To nije propust nego posljedica toga šta se u pretrazi uopšte može tražiti.

### Sličnost

| Signal | Težina |
|---|---|
| tip vozila | 0,35 |
| cjenovni rang | 0,25 |
| grad / poslovnica | 0,20 |
| marka | 0,10 |
| kubikažni razred | 0,10 |

```
sličnost = Σ (težina_i × poklapanje_i) / Σ težina_i
```

Dijeli se zbirom težina **onih signala o kojima profil zaista ima podatak**. Korisnik
koji je tražio samo tip vozila nema podatak o marki ni gradu; kad bi se nepoznati
signali računali kao nula, njegova sličnost bila bi najviše 0,35 — vozilo bi bilo
kažnjeno zato što korisnik o tome nikad nije ostavio trag. Udio nula i nepoznat signal
su dvije različite stvari.

Blizina cijene:

```
poklapanje = ograniči(1 − |dnevnaTarifa − prosječnaCijena| / prosječnaCijena, 0, 1)
```

Kubikažni razredi: do 50 · 51–125 · 126–500 · preko 500 cm³.

### Popularnost

```
popularnost = 0,5 × učestalost + 0,5 × ocjena

učestalost = brojNajmova / najveciBrojNajmovaUFloti
bayes      = (5 × prosjekFlote + zbirOcjena) / (5 + brojOcjena)
ocjena     = (bayes − 1) / 4
```

Broje se rezervacije čiji datum preuzimanja pada u zadnjih **90 dana**, u statusu
`Confirmed` ili `Completed`. Otkazane se ne broje — one ne govore da je vozilo bilo
traženo nego da nije bilo uzeto.

Bayesov prosjek povlači vozila sa malo ocjena prema prosjeku flote, pa jedna petica daje
4,17 umjesto 5,00. Bez toga bi vozilo sa jednom ocjenom preskočilo vozilo sa četrdeset
ocjena i prosjekom 4,7.

Korisnik bez ijednog signala u profilu dobija **čistu popularnost**, bez množenja sa
0,4 — množenje bi svim vozilima podjednako smanjilo skor, poredak bi ostao isti, a
brojevi bi tvrdili nešto što model u tom slučaju ne radi.

### Obrazloženje

Rečenica se gradi od signala koji je najviše doprinio skoru. Doprinosi se porede tek
pomnoženi svojim udjelom u konačnom skoru: popularnost od 0,875 ulazi kao 0,4 × 0,875 =
0,35, a savršeno poklopljen tip kao 0,6 × 1 = 0,6 — pa pobjeđuje tip, iako je sirovi
broj manji.

| Signal | Rečenica |
|---|---|
| tip | „Najčešće birate skuter." |
| cijena | „Cijena 40,00 EUR po danu je u rangu koji obično tražite." |
| lokacija | „Vozilo je u poslovnici Mostar centar, u gradu u kojem najčešće tražite." |
| marka | „Marku Yamaha ste već birali." |
| kubikaža | „Kubikaža 125 cm³ odgovara vozilima koja ste ranije iznajmljivali." |
| ocjena | „Visoko ocijenjeno: 4,6 od 5, iz 12 recenzija." |
| popularnost | „Jedno od najtraženijih vozila: 8 najmova u zadnja tri mjeseca." |

### Kada se pretraga bilježi

`HistorijaPretrageService.ZabiljeziAsync` se poziva iz `VoziloService.GetAsync`, iz
jedine metode kroz koju pretraga stvarno prolazi. Zapis se **ne** upisuje u tri slučaja:

1. **Pretražuje osoblje** — administrator i uposlenik pretražuju flotu zbog posla.
2. **Zahtjev nema nijedan filter** — otvaranje liste nije pretraga, a red samih praznih
   vrijednosti ne može podići nijedan skor.
3. **Druga i dalje stranica** — listanje je ista pretraga, pa bi se isti ukus prebrojao
   onoliko puta koliko korisnik ima strpljenja.

Neuspio upis se logira i ne obara pretragu.

---

# Dio 3 — Zajedničko

## 9. Filteri

Oba puta bodovanja rade **samo nad vozilima koja klijent stvarno može rezervisati**:

| Uslov | Kako |
|---|---|
| vozilo je aktivno | `Vozilo.Aktivno` |
| klijent ga smije voziti | `IDozvolaService.DozvoljeneKategorijeIdAsync` |
| slobodno je u traženom terminu | `IAvailabilityService.DodajUslovSlobodno` |
| poslovnica, grad, tip vozila | kad ih zahtjev navede |

Oba ključna filtera koriste **iste servise** koje koriste pretraga i kreiranje
rezervacije. Da preporuke imaju vlastitu provjeru, klijentu bi se moglo ponuditi vozilo
koje mu rezervacija odbija. Rok važenja dozvole provjerava se na datum preuzimanja.

---

## 10. Endpointi

| Metoda | Ruta | Šta radi | Ko |
|---|---|---|---|
| GET | `/api/preporuke` | preporuke za prijavljenog korisnika | klijent |
| GET | `/api/preporuke/slicna/{voziloId}` | vozila slična zadatom | klijent |
| GET | `/api/preporuke/model` | stanje i greška modela | administrator |
| POST | `/api/preporuke/model/treniraj` | treniranje na zahtjev | administrator |

Odgovor je paginiran, sa gornjom granicom **20** umjesto 100 — preporuka koja je
dvadeseta po redu više nije preporuka nego katalog.

Svaka stavka nosi `metoda`, `skor`, i — zavisno od puta — `predvidjenaOcjena` ili
`slicnost` i `popularnost`, pa se na odbrani može pokazati tačno odakle je broj došao.

### Slična vozila

Ovo se namjerno **ne** računa modelom. Matrična faktorizacija uči ukus korisnika, a
ovdje se pita nešto drugo: koje vozilo liči na ovo. To je poređenje atributa, pa ide
kroz račun sličnosti, sa profilom sastavljenim od samog vozila koje korisnik gleda —
svaki signal ima udio 1 na vrijednosti tog vozila. Vozila istog modela su izuzeta, jer
četiri puta ista Vespa nije prijedlog.

---

## 11. Šta sistem namjerno **ne** radi

- **Nema učenja iz klikova na preporuku.** Bilježi se pretraga, ocjena i završen najam,
  ne i to koju je preporuku korisnik otvorio.
- **Nema vremenskog slabljenja starih signala.** Ocjena od prije godinu dana vrijedi
  isto kao jučerašnja.
- **Model se ne trenira inkrementalno.** Svako osvježavanje je treniranje od nule.
- **Model se ne čuva na disk.** Poslije restarta se trenira ponovo, što na ovoj količini
  podataka traje kraće od podizanja baze.
- **Recenzije ne ulaze u sličnost** na rezervnom putu, samo u popularnost. Ocjena je sud
  o vozilu, ne opis ukusa.

---

## 12. Testovi

Oba čista dijela — priprema podataka za model i rezervni račun — pokrivena su testovima
bez baze i bez ML.NET-a.

**`MjereTests`** (7 slučajeva)

| Šta se provjerava |
|---|
| savršena predikcija nema grešku, R² je 1 |
| RMSE kažnjava veliku grešku više nego MAE |
| pogađanje prosjeka daje R² nula, lošije od toga daje negativan |
| osnovna greška je greška pogađanja prosjeka |
| iste ocjene ne dijele nulom |

**`PripremaInterakcijaTests`** (16 slučajeva)

| Šta se provjerava |
|---|
| ocjena postaje red sa oznakom stvarne |
| više ocjena istog para ulazi kao prosjek |
| najam bez recenzije ulazi sa prosjekom flote, bez oznake stvarne |
| ocjena ima prednost nad procjenom za isti par |
| prag: premalo ocjena, premalo korisnika, dovoljno oboje |
| procijenjeni redovi se ne računaju u prag |
| u test skup idu samo stvarne ocjene |
| podjela je uvijek ista |
| podjela ne ostavlja premalo za učenje |
| u unakrsnoj provjeri svaka ocjena tačno jednom bude u skupu za provjeru |
| procijenjeni redovi uvijek idu u učenje |
| premalo ocjena znači da unakrsne provjere nema |

**`BodovanjePreporukeTests`** (27 slučajeva)

| Šta se provjerava |
|---|
| granice kubikažnih razreda |
| učestalost: najtraženije vozilo daje 1, prazna flota daje 0 |
| Bayes: vozilo bez recenzija dobija prosjek flote; jedna petica ne preskače 40 ocjena |
| potpuno poklapanje daje sličnost 1, nikakvo daje 0 |
| nepoznat signal ne ulazi u račun |
| tip nosi veći udio od marke, tačno 0,35 : 0,10 |
| blizina cijene pada linearno i nikad ispod nule |
| skor je 0,6 : 0,4, a zbir težina 1 |
| korisnik bez historije dobija čistu popularnost |
| obrazloženje bira signal po doprinosu konačnom skoru |

---

## 13. Historija odluke o metodi

Prva verzija sistema bila je isključivo ponderisano bodovanje sa unaprijed zadatim
težinama. Mentor je na prijavu teme odgovorio da content-based nije naziv algoritma nego
pristup, i da je sistem sa zadatim težinama scoring heuristika a ne ML model, te tražio
da se uvede stvarna metoda mašinskog učenja.

Zato je uvedena matrična faktorizacija: postoji faza učenja, parametri se izvode iz
podataka, i model se mjeri greškom na podacima koje pri učenju nije vidio. Ranije
bodovanje nije obrisano nego je svedeno na ono što jedino i može biti — rješenje za
hladni start, jasno označeno kao takvo.

---

*Dokument prati zahtjeve iz „Razvoj softvera II — Upute za izradu seminarskog rada", 10.03.2026.*
