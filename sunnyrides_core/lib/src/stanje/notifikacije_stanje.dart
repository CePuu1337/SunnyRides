import 'package:flutter/foundation.dart';

import '../api/api_greska.dart';
import '../api/api_klijent.dart';
import '../api/okruzenje.dart';
import '../auth/pohrana_tokena.dart';
import '../modeli/pretvaranje.dart';
import '../realtime/veza_notifikacija.dart';

/// Broj neprocitanih obavjestenja, za oznaku na zvonu.
///
/// Broj dolazi sa dva puta: zahtjevom pri otvaranju aplikacije i gurnut sa servera
/// kroz hub. Polje je isto, pa ekran ne mora znati odakle je stigao.
class NotifikacijeStanje extends ChangeNotifier {
  NotifikacijeStanje({
    required this._klijent,
    required Okruzenje okruzenje,
    required PohranaTokena pohrana,
  })  : _veza = VezaNotifikacija(okruzenje: okruzenje, pohrana: pohrana) {
    _veza.naBrojNeprocitanih = postavi;
    _veza.naNovuNotifikaciju = (notifikacija) => naNovuNotifikaciju?.call(notifikacija);
  }

  final ApiKlijent _klijent;
  final VezaNotifikacija _veza;

  /// Ekran koji prikazuje listu obavjestenja moze se prikljuciti ovdje i dopuniti se
  /// bez novog zahtjeva.
  void Function(Map<String, dynamic> notifikacija)? naNovuNotifikaciju;

  int _neprocitanih = 0;

  int get neprocitanih => _neprocitanih;

  /// Prvi broj se uzima zahtjevom, jer hub javlja samo promjene - obavjestenja
  /// nastala prije povezivanja inace se ne bi vidjela.
  Future<void> pokreni() async {
    await osvjezi();
    await _veza.poveziSe();
  }

  Future<void> osvjezi() async {
    try {
      final odgovor = await _klijent.get('/api/notifikacije/broj-neprocitanih');

      if (odgovor is Map<String, dynamic>) {
        postavi(citajInt(odgovor['broj']));
      }
    } on ApiGreska {
      // Broj na zvonu nije razlog da se korisniku prikaze greska. Ostaje zadnji
      // poznati broj i pokusava se ponovo pri sljedecem osvjezavanju.
    }
  }

  void postavi(int broj) {
    if (broj == _neprocitanih) {
      return;
    }

    _neprocitanih = broj;
    notifyListeners();
  }

  @override
  void dispose() {
    _veza.raskini();
    super.dispose();
  }
}
