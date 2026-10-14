import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';

import '../modeli/tacka.dart';

/// Trenutna lokacija telefona, kad je klijent sam zatrazi.
///
/// Osnova je grad iz profila. Lokacija je dodatak za klijenta koji nije kod kuce:
/// cita se tek na dodir dugmeta, jednom, i nigdje se ne cuva - salje se uz
/// pretragu i zaboravlja kad se klijent odjavi. Klijent u dijalogu dozvole sam
/// bira da li daje preciznu ili samo priblizno lokaciju; obje su dovoljne.
class Lokacija extends ChangeNotifier {
  Tacka? _tacka;
  bool _trazenje = false;

  Tacka? get tacka => _tacka;
  bool get ukljucena => _tacka != null;
  bool get trazenje => _trazenje;

  /// Trazi lokaciju od telefona. Vraca poruku za korisnika kad nije uspjelo, a
  /// nista kad jeste.
  Future<String?> ukljuci() async {
    if (_trazenje) {
      return null;
    }

    _trazenje = true;
    notifyListeners();

    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        return 'Lokacija je isključena na telefonu. Uključite je u postavkama, '
            'pa pokušajte ponovo.';
      }

      var dozvola = await Geolocator.checkPermission();

      if (dozvola == LocationPermission.denied) {
        dozvola = await Geolocator.requestPermission();
      }

      if (dozvola == LocationPermission.deniedForever) {
        return 'Pristup lokaciji je odbijen. Možete ga dozvoliti u postavkama '
            'aplikacije, a do tada se udaljenost mjeri od vašeg grada.';
      }

      if (dozvola == LocationPermission.denied) {
        return 'Bez dozvole za lokaciju udaljenost se mjeri od vašeg grada.';
      }

      final polozaj = await _polozaj();

      if (polozaj == null) {
        return 'Telefon još ne zna gdje je. Pokušajte ponovo za koji trenutak.';
      }

      _tacka = Tacka(polozaj.latitude, polozaj.longitude);

      return null;
    } on Exception {
      return 'Lokacija trenutno nije dostupna, pa se udaljenost mjeri od vašeg '
          'grada.';
    } finally {
      _trazenje = false;
      notifyListeners();
    }
  }

  /// Ponovo cita lokaciju ako je ukljucena - za povlacenje za osvjezavanje, kad
  /// se klijent u medjuvremenu pomjerio. Kad nije ukljucena, ne radi nista.
  Future<String?> osvjezi() async => ukljucena ? ukljuci() : null;

  /// Nazad na grad iz profila.
  void iskljuci() {
    if (_tacka == null) {
      return;
    }

    _tacka = null;
    notifyListeners();
  }

  /// Trazi se svjez polozaj, ne zapamceni - inace bi klijent koji se pomjerio
  /// dobio najblizu poslovnicu od mjesta gdje je bio. Cita se jednom, pa trosak
  /// baterije ne igra ulogu. Ako se ne javi na vrijeme, uzima se zadnji poznati,
  /// ako ga telefon ima.
  static Future<Position?> _polozaj() async {
    try {
      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 15),
        ),
      );
    } on TimeoutException {
      return Geolocator.getLastKnownPosition();
    }
  }
}
