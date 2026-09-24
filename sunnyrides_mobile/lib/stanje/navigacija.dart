import 'package:flutter/foundation.dart';

/// Sta pretraga treba otvoriti kad je pozove neki drugi ekran.
class ZahtjevPretrage {
  const ZahtjevPretrage({this.tekst, this.tipVozilaId});

  final String? tekst;
  final int? tipVozilaId;
}

/// Prebacivanje izmedju kartica donje trake, izvan ljuske.
///
/// Pocetni ekran ima polje za pretragu i dugmad za tipove vozila, a pretraga je
/// zasebna kartica. Bez ovoga bi pocetni ekran morao znati kako je ljuska
/// sastavljena; ovako samo kaze sta hoce, a ljuska to izvrsi.
class Navigacija extends ChangeNotifier {
  int _kartica = 0;
  ZahtjevPretrage? _pretraga;

  int get kartica => _kartica;

  void otvoriKarticu(int indeks) {
    if (indeks == _kartica) {
      return;
    }

    _kartica = indeks;
    notifyListeners();
  }

  void otvoriPretragu({String? tekst, int? tipVozilaId}) {
    _pretraga = ZahtjevPretrage(tekst: tekst, tipVozilaId: tipVozilaId);
    _kartica = 1;
    notifyListeners();
  }

  /// Zahtjev se cita samo jednom - inace bi se isti filter primijenio i kad se
  /// korisnik kasnije sam vrati na pretragu.
  ZahtjevPretrage? preuzmiZahtjev() {
    final zahtjev = _pretraga;
    _pretraga = null;

    return zahtjev;
  }
}
