import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'aplikacija.dart';
import 'stanje/sesija.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // Adresa API-ja se moze nadjacati pri pokretanju:
  //   flutter run -d windows --dart-define=API_BASE_URL=http://192.168.0.10:5000
  final okruzenje = Okruzenje.izDefinicija(
    podrazumijevanaAdresa: 'http://localhost:5000',
  );

  final pohranaTokena = PohranaTokena();
  final apiKlijent = ApiKlijent(
    okruzenje: okruzenje,
    pohranaTokena: pohranaTokena,
  );
  final authServis = AuthServis(klijent: apiKlijent, pohrana: pohranaTokena);
  final sesija = Sesija(authServis: authServis, pohranaTokena: pohranaTokena);

  // Kad server odbije token, aplikacija se sama vraca na prijavu. Bez ovoga bi
  // korisnik ostao na ekranu koji vise nema podataka i svaki bi klik javljao gresku.
  apiKlijent.naIstekSesije = sesija.sesijaIstekla;

  runApp(
    MultiProvider(
      providers: [
        Provider<Okruzenje>.value(value: okruzenje),
        Provider<ApiKlijent>.value(value: apiKlijent),
        ChangeNotifierProvider<Sesija>.value(value: sesija),
      ],
      child: const DesktopAplikacija(),
    ),
  );

  // Provjera sacuvanog tokena ide nakon runApp, da se prozor odmah pojavi umjesto
  // da korisnik gleda u prazno dok traje mrezni poziv.
  sesija.pokreni();
}
