import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'aplikacija.dart';
import 'stanje/sesija.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // Emulator host masinu vidi kao 10.0.2.2, ne kao localhost. Na stvarnom telefonu
  // se adresa zadaje pri pokretanju:
  //   flutter run --dart-define=API_BASE_URL=http://192.168.0.10:5000
  // i ta adresa se dopise u res/xml/network_security_config.xml - Android obican
  // HTTP pusta samo prema adresama koje su tamo navedene.
  final okruzenje = Okruzenje.izDefinicija(
    podrazumijevanaAdresa: 'http://10.0.2.2:5000',
  );

  final pohranaTokena = PohranaTokena();
  final apiKlijent = ApiKlijent(
    okruzenje: okruzenje,
    pohranaTokena: pohranaTokena,
  );
  final authServis = AuthServis(klijent: apiKlijent, pohrana: pohranaTokena);
  final sesija = Sesija(authServis: authServis, pohranaTokena: pohranaTokena);

  apiKlijent.naIstekSesije = sesija.sesijaIstekla;

  runApp(
    MultiProvider(
      providers: [
        Provider<Okruzenje>.value(value: okruzenje),
        Provider<ApiKlijent>.value(value: apiKlijent),
        Provider<PohranaTokena>.value(value: pohranaTokena),
        Provider<AuthServis>.value(value: authServis),
        ChangeNotifierProvider<Sesija>.value(value: sesija),
      ],
      child: const MobilnaAplikacija(),
    ),
  );

  sesija.pokreni();
}
