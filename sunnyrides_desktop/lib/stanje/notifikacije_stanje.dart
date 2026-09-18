import 'package:flutter/foundation.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Broj neprocitanih obavjestenja, za oznaku na zvonu.
///
/// Zasad se dohvata zahtjevom. Kad se prikljuci SignalR, isti broj ce stizati i
/// gurnut sa servera - polje je isto, pa se ovdje mijenja samo izvor.
class NotifikacijeStanje extends ChangeNotifier {
  NotifikacijeStanje(this._klijent);

  final ApiKlijent _klijent;

  int _neprocitanih = 0;

  int get neprocitanih => _neprocitanih;

  Future<void> osvjezi() async {
    try {
      final odgovor = await _klijent.get('/api/notifikacije/broj-neprocitanih');

      if (odgovor is Map<String, dynamic>) {
        _neprocitanih = citajInt(odgovor['broj']);
        notifyListeners();
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
}
