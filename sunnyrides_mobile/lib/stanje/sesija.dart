import 'package:flutter/foundation.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

enum StanjeSesije { provjera, neprijavljen, prijavljen }

/// Ko je prijavljen u mobilnoj aplikaciji.
class Sesija extends ChangeNotifier {
  Sesija({
    required AuthServis authServis,
    required PohranaTokena pohranaTokena,
    required this._mojaDozvola,
  }) : _auth = authServis,
       _pohrana = pohranaTokena;

  final AuthServis _auth;
  final PohranaTokena _pohrana;
  final Future<VozackaDozvola?> Function() _mojaDozvola;

  StanjeSesije _stanje = StanjeSesije.provjera;
  Korisnik? _korisnik;
  String? _porukaOdjave;

  /// Da li je klijent prijavio dozvolu sa obje fotografije. Null znaci da se ne zna
  /// (provjera nije uspjela) - tada se korak ne namece, jer bez dozvole ionako nema
  /// rezervacije: to pravilo provjerava server, a ovo je samo vodjenje kroz unos.
  bool? _dozvolaPrijavljena;

  StanjeSesije get stanje => _stanje;
  Korisnik? get korisnik => _korisnik;

  /// Klijent koji jos nije prijavio dozvolu prolazi kroz nju odmah poslije grada,
  /// prije ulaska u aplikaciju. Prijava teme dozvolu stavlja u registraciju, a ne
  /// kao nesto sto se moze, ali i ne mora, uraditi kasnije u profilu.
  bool get trebaPrijavitiDozvolu =>
      _stanje == StanjeSesije.prijavljen &&
      (_korisnik?.jeKlijent ?? false) &&
      _dozvolaPrijavljena == false;

  /// Pravilo kada je dozvola "prijavljena": postoje podaci i obje fotografije.
  /// Status se ne gleda - odbijena ili dozvola na cekanju je prijavljena, a njenu
  /// izmjenu klijent radi iz profila, ne kroz ovaj korak.
  static bool jePrijavljena(VozackaDozvola? dozvola) =>
      dozvola != null && dozvola.imaObjeStrane;

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

      await _provjeriDozvolu();
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

    await _provjeriDozvolu();
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

    // Nov nalog nema dozvolu, pa je ne treba ni pitati server.
    _dozvolaPrijavljena = false;
    _postavi(StanjeSesije.prijavljen, korisnik);
  }

  Future<void> odjava() async {
    await _auth.odjava();
    _postavi(StanjeSesije.neprijavljen, null);
  }

  void sesijaIstekla([String? poruka]) {
    if (_stanje != StanjeSesije.prijavljen) {
      return;
    }

    _porukaOdjave = poruka ?? 'Sesija je istekla. Prijavite se ponovo.';
    _postavi(StanjeSesije.neprijavljen, null);
  }

  void osvjeziKorisnika(Korisnik korisnik) {
    _korisnik = korisnik;
    notifyListeners();
  }

  /// Zove ga korak dozvole kad su podaci i obje fotografije poslani. Korijen
  /// aplikacije tada sam prelazi na pocetni ekran.
  void dozvolaPoslana(VozackaDozvola dozvola) {
    _dozvolaPrijavljena = jePrijavljena(dozvola);
    notifyListeners();
  }

  Future<void> _provjeriDozvolu() async {
    try {
      _dozvolaPrijavljena = jePrijavljena(await _mojaDozvola());
    } on ApiGreska {
      _dozvolaPrijavljena = null;
    }
  }

  void _postavi(StanjeSesije stanje, Korisnik? korisnik) {
    _stanje = stanje;
    _korisnik = korisnik;

    if (stanje != StanjeSesije.prijavljen) {
      _dozvolaPrijavljena = null;
    }

    notifyListeners();
  }

  static const _porukaZaOsoblje =
      'Ovaj nalog pripada osoblju agencije. Mobilna aplikacija je za klijente, a '
      'osoblje radi kroz desktop aplikaciju.';
}
