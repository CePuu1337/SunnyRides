# SunnyRides — desktop aplikacija

Windows aplikacija za administratore i uposlenike agencije: pregled poslovanja, vozila,
rezervacije, kalendar flote sa blokadama i zamjenom vozila, primopredaja, verifikacija
vozačkih dozvola, recenzije, obavijesti, korisnici, šifarnici, cjenovnik i PDF
izvještaji.

Zajednički dio (API klijent, modeli, tema, SignalR veza) je u paketu
`../sunnyrides_core`.

## Pokretanje

API mora raditi (vidi `README.md` u korijenu repozitorija).

```bash
flutter pub get
flutter run -d windows
```

Podrazumijevana adresa API-ja je `http://localhost:5000`. Druga se zadaje pri
pokretanju i čita kroz `String.fromEnvironment('API_BASE_URL')`:

```bash
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

Prijava: `desktop` / `test` ili `administrator` / `test` (sve), `uposlenik` / `test`
(bez cjenovnika, korisnika i šifarnika).

## Build za predaju

```bash
flutter clean
flutter build windows --release
```

Rezultat je u `build/windows/x64/runner/Release/`.
