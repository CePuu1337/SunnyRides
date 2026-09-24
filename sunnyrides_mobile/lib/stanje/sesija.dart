import 'package:flutter/foundation.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

enum StanjeSesije { provjera, neprijavljen, prijavljen }

/// Ko je prijavljen u mobilnoj aplikaciji.
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

  String? preuzmiPorukuOdjave() {
    final poruka = _porukaOdjave;
    _porukaOdjave = null;

    return poruka;
  }

  Future<void> pokreni() async {
    await _pohrana.ucitaj();

    if (!_pohrana.imaVazeciToken) {
      _postavi(StanjeSesije.neprijavljen, null);

      return;
    }

    try {
      final korisnik = await _auth.ja();

      if (!korisnik.jeKlijent) {
        await _auth.odjava();
        _porukaOdjave = _porukaZaOsoblje;
        _postavi(StanjeSesije.neprijavljen, null);

        return;
      }

      _postavi(StanjeSesije.prijavljen, korisnik);
    } on ApiGreska {
      await _pohrana.obrisi();
      _postavi(StanjeSesije.neprijavljen, null);
    }
  }

  Future<void> prijava(String korisnickoIme, String lozinka) async {
    final korisnik = await _auth.prijava(korisnickoIme, lozinka);

    // Aplikacija je za klijente. Nalog osoblja bi ovdje vidio ekrane koji mu ne
    // znace nista, a server bi mu vecinu poziva ionako odbio.
    if (!korisnik.jeKlijent) {
      await _auth.odjava();

      throw ApiGreska(
        status: 403,
        poruka: _porukaZaOsoblje,
        naslov: 'Pogrešna aplikacija',
      );
    }

    _postavi(StanjeSesije.prijavljen, korisnik);
  }

  Future<void> registracija({
    required String korisnickoIme,
    required String ime,
    required String prezime,
    required String email,
    required String telefon,
    required DateTime datumRodjenja,
    required String lozinka,
    required String potvrdaLozinke,
  }) async {
    final korisnik = await _auth.registracija(
      korisnickoIme: korisnickoIme,
      ime: ime,
      prezime: prezime,
      email: email,
      telefon: telefon,
      datumRodjenja: datumRodjenja,
      lozinka: lozinka,
      potvrdaLozinke: potvrdaLozinke,
    );

    _postavi(StanjeSesije.prijavljen, korisnik);
  }

  Future<void> odjava() async {
    await _auth.odjava();
    _postavi(StanjeSesije.neprijavljen, null);
  }

  void sesijaIstekla() {
    if (_stanje != StanjeSesije.prijavljen) {
      return;
    }

    _porukaOdjave = 'Sesija je istekla. Prijavite se ponovo.';
    _postavi(StanjeSesije.neprijavljen, null);
  }

  void osvjeziKorisnika(Korisnik korisnik) {
    _korisnik = korisnik;
    notifyListeners();
  }

  void _postavi(StanjeSesije stanje, Korisnik? korisnik) {
    _stanje = stanje;
    _korisnik = korisnik;
    notifyListeners();
  }

  static const _porukaZaOsoblje =
      'Ovaj nalog pripada osoblju agencije. Mobilna aplikacija je za klijente, a '
      'osoblje radi kroz desktop aplikaciju.';
}
