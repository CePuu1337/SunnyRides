import 'package:flutter/foundation.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

enum StanjeSesije {
  /// Provjerava se sacuvani token. Traje samo pri pokretanju.
  provjera,
  neprijavljen,
  prijavljen,
}

/// Ko je prijavljen i sta aplikacija smije prikazati.
class Sesija extends ChangeNotifier {
  Sesija({required AuthServis authServis, required PohranaTokena pohranaTokena})
    : _auth = authServis,
      _pohrana = pohranaTokena;

  final AuthServis _auth;
  final PohranaTokena _pohrana;

  StanjeSesije _stanje = StanjeSesije.provjera;
  Korisnik? _korisnik;
  String? _porukaOdjave;

  StanjeSesije get stanje => _stanje;
  Korisnik? get korisnik => _korisnik;

  /// Razlog zbog kojeg je korisnik zavrsio na ekranu prijave, ako ga ima.
  /// Ekran ga prikaze jednom i obrise - inace bi stajao i poslije uspjesne prijave.
  String? preuzmiPorukuOdjave() {
    final poruka = _porukaOdjave;
    _porukaOdjave = null;

    return poruka;
  }

  /// Provjera sacuvanog tokena pri pokretanju.
  Future<void> pokreni() async {
    await _pohrana.ucitaj();

    if (!_pohrana.imaVazeciToken) {
      _postavi(StanjeSesije.neprijavljen, null);

      return;
    }

    try {
      final korisnik = await _auth.ja();

      if (!korisnik.jeOsoblje) {
        // Token je ispravan, ali pripada klijentu. Desktop aplikacija je za osoblje.
        await _auth.odjava();
        _porukaOdjave = _porukaZaKlijenta;
        _postavi(StanjeSesije.neprijavljen, null);

        return;
      }

      _postavi(StanjeSesije.prijavljen, korisnik);
    } on ApiGreska {
      // Token vise ne vrijedi - odjavom ponisten ili promijenjena lozinka.
      await _pohrana.obrisi();
      _postavi(StanjeSesije.neprijavljen, null);
    }
  }

  /// Prijava. Greske se ne hvataju ovdje nego na ekranu, koji ih ima gdje prikazati.
  Future<void> prijava(String korisnickoIme, String lozinka) async {
    final korisnik = await _auth.prijava(korisnickoIme, lozinka);

    if (!korisnik.jeOsoblje) {
      await _auth.odjava();

      throw ApiGreska(
        status: 403,
        poruka: _porukaZaKlijenta,
        naslov: 'Pristup nije dozvoljen',
      );
    }

    _postavi(StanjeSesije.prijavljen, korisnik);
  }

  Future<void> odjava() async {
    await _auth.odjava();
    _postavi(StanjeSesije.neprijavljen, null);
  }

  /// Poziva se iz API klijenta kad server vrati 401.
  void sesijaIstekla() {
    if (_stanje != StanjeSesije.prijavljen) {
      return;
    }

    _porukaOdjave = 'Sesija je istekla. Prijavite se ponovo.';
    _postavi(StanjeSesije.neprijavljen, null);
  }

  /// Osvjezavanje podataka o prijavljenom korisniku, nakon izmjene profila.
  void osvjeziKorisnika(Korisnik korisnik) {
    _korisnik = korisnik;
    notifyListeners();
  }

  void _postavi(StanjeSesije stanje, Korisnik? korisnik) {
    _stanje = stanje;
    _korisnik = korisnik;
    notifyListeners();
  }

  static const _porukaZaKlijenta =
      'Ovaj nalog je klijentski. Desktop aplikacija je namijenjena osoblju agencije, '
      'a za klijente postoji mobilna aplikacija.';
}
