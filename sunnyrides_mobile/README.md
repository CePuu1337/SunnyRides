# SunnyRides — mobilna aplikacija

Android aplikacija za klijente: preporuke, pretraga slobodnih vozila sa filterima i
ukupnom cijenom za termin, rezervacija sa opremom i osiguranjem, plaćanje kroz Stripe
PaymentSheet, otkazivanje sa obračunom povrata, vozačka dozvola, recenzije, profil,
reset zaboravljene lozinke i obavještenja u realnom vremenu.

Zajednički dio (API klijent, modeli, tema, SignalR veza) je u paketu
`../sunnyrides_core`.

## Pokretanje

API mora raditi (vidi `README.md` u korijenu repozitorija), a Android emulator biti
upaljen.

```bash
flutter pub get
flutter run
```

Podrazumijevana adresa API-ja je `http://10.0.2.2:5000` — tako emulator vidi
`localhost` računara. Druga se zadaje pri pokretanju i čita kroz
`String.fromEnvironment('API_BASE_URL')`:

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Prijava: `mobile` / `test`. Testna kartica: `4242 4242 4242 4242`, bilo koji budući
datum, CVC `123`.

## Build za predaju

```bash
flutter clean
flutter build apk --release
```

Rezultat je `build/app/outputs/flutter-apk/app-release.apk`.
