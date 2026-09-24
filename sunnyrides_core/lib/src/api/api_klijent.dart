import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../auth/pohrana_tokena.dart';
import 'api_greska.dart';
import 'okruzenje.dart';

/// Jedino mjesto u aplikaciji koje zna kako izgleda HTTP poziv prema API-ju.
///
/// Ekrani ne sastavljaju adrese, ne dodaju zaglavlja i ne citaju statuse - oni zovu
/// servis, servis zove ovaj klijent. Kad se promijeni nacin autentifikacije ili
/// oblik greske, mijenja se jedan fajl.
class ApiKlijent {
  ApiKlijent({
    required this.okruzenje,
    required PohranaTokena pohranaTokena,
    http.Client? klijent,
  })  : _pohrana = pohranaTokena,
        _klijent = klijent ?? http.Client();

  final Okruzenje okruzenje;
  final PohranaTokena _pohrana;
  final http.Client _klijent;

  /// Poziva se kad server odbije token. Aplikacija na to vraca korisnika na prijavu.
  void Function()? naIstekSesije;

  Future<dynamic> get(String putanja, {Map<String, dynamic>? upit}) async {
    return _posalji(() => _klijent.get(_adresa(putanja, upit), headers: _zaglavlja()));
  }

  Future<dynamic> post(String putanja, {Object? tijelo, Map<String, dynamic>? upit}) async {
    return _posalji(() => _klijent.post(
          _adresa(putanja, upit),
          headers: _zaglavlja(saTijelom: true),
          body: tijelo == null ? null : jsonEncode(tijelo),
        ));
  }

  Future<dynamic> put(String putanja, {Object? tijelo}) async {
    return _posalji(() => _klijent.put(
          _adresa(putanja, null),
          headers: _zaglavlja(saTijelom: true),
          body: tijelo == null ? null : jsonEncode(tijelo),
        ));
  }

  Future<dynamic> delete(String putanja) async {
    return _posalji(() => _klijent.delete(_adresa(putanja, null), headers: _zaglavlja()));
  }

  /// Dohvat sadrzaja koji nije JSON - fotografija dozvole, PDF izvjestaja.
  ///
  /// Ide kroz isti klijent jer i ti endpointi traze token: fotografija vozacke
  /// dozvole se ne posluzuje kao staticki fajl nego kroz provjeru vlasnistva.
  Future<Uint8List> bajtovi(String putanja, {Map<String, dynamic>? upit}) async {
    final odgovor = await _uhvatiMreznu(
      () => _klijent.get(_adresa(putanja, upit), headers: _zaglavlja()),
    );

    if (odgovor.statusCode == 401) {
      await _zavrsiSesiju();
    }

    if (odgovor.statusCode >= 400) {
      throw _uGresku(odgovor);
    }

    return odgovor.bodyBytes;
  }

  /// Otprema fajla kao multipart. Sadrzaj nikad ne ide kao base64 u JSON-u.
  Future<dynamic> posaljiFajl(
    String putanja, {
    required String nazivPolja,
    required String imeFajla,
    required List<int> sadrzaj,
  }) async {
    final zahtjev = http.MultipartRequest('POST', _adresa(putanja, null))
      ..headers.addAll(_zaglavlja())
      ..files.add(http.MultipartFile.fromBytes(nazivPolja, sadrzaj, filename: imeFajla));

    return _posalji(() async {
      final tok = await _klijent.send(zahtjev);

      return http.Response.fromStream(tok);
    });
  }

  /// Forma sa poljima i vise fajlova odjednom.
  ///
  /// Koristi se tamo gdje podaci i fotografije cine jedan zapis - izdavanje i povrat
  /// vozila. Da se salju odvojeno, postojao bi trenutak u kojem je povrat upisan a
  /// fotografije stete jos nisu, pa bi pravilo "steta mora imati fotografiju" imalo
  /// rupu kroz koju se prolazi tako sto se drugi zahtjev jednostavno ne posalje.
  Future<dynamic> posaljiFormu(
    String putanja, {
    required Map<String, dynamic> polja,
    String nazivPoljaFajlova = 'fotografije',
    List<FajlZaSlanje> fajlovi = const [],
  }) async {
    final zahtjev = http.MultipartRequest('POST', _adresa(putanja, null))
      ..headers.addAll(_zaglavlja());

    for (final unos in polja.entries) {
      final vrijednost = unos.value;

      if (vrijednost == null) {
        continue;
      }

      zahtjev.fields[unos.key] = vrijednost is DateTime
          ? vrijednost.toUtc().toIso8601String()
          : vrijednost.toString();
    }

    for (final fajl in fajlovi) {
      zahtjev.files.add(
        http.MultipartFile.fromBytes(
          nazivPoljaFajlova,
          fajl.sadrzaj,
          filename: fajl.ime,
        ),
      );
    }

    return _posalji(() async {
      final tok = await _klijent.send(zahtjev);

      return http.Response.fromStream(tok);
    });
  }

  void zatvori() => _klijent.close();

  // --- unutrasnjost ------------------------------------------------------

  Uri _adresa(String putanja, Map<String, dynamic>? upit) {
    final puna = putanja.startsWith('/') ? putanja : '/$putanja';
    final adresa = Uri.parse(okruzenje.osnovnaAdresa + puna);

    if (upit == null || upit.isEmpty) {
      return adresa;
    }

    return adresa.replace(queryParameters: _upitUTekst(upit));
  }

  /// Vrijednosti upita se pretvaraju u tekst ovdje, na jednom mjestu.
  ///
  /// Datumi idu u UTC i ISO obliku, jer server radi iskljucivo u UTC-u. Prazne
  /// vrijednosti se izostavljaju - filter koji nije postavljen ne treba ni slati.
  Map<String, dynamic> _upitUTekst(Map<String, dynamic> upit) {
    final rezultat = <String, dynamic>{};

    for (final unos in upit.entries) {
      final vrijednost = unos.value;

      if (vrijednost == null) {
        continue;
      }

      if (vrijednost is DateTime) {
        rezultat[unos.key] = vrijednost.toUtc().toIso8601String();
      } else if (vrijednost is List) {
        final stavke = vrijednost.map((x) => x.toString()).toList();

        if (stavke.isNotEmpty) {
          rezultat[unos.key] = stavke;
        }
      } else {
        final tekst = vrijednost.toString();

        if (tekst.isNotEmpty) {
          rezultat[unos.key] = tekst;
        }
      }
    }

    return rezultat;
  }

  Map<String, String> _zaglavlja({bool saTijelom = false}) {
    final zaglavlja = <String, String>{'Accept': 'application/json'};

    if (saTijelom) {
      zaglavlja['Content-Type'] = 'application/json';
    }

    final token = _pohrana.token;

    if (token != null && token.isNotEmpty) {
      zaglavlja['Authorization'] = 'Bearer $token';
    }

    return zaglavlja;
  }

  Future<dynamic> _posalji(Future<http.Response> Function() poziv) async {
    final odgovor = await _uhvatiMreznu(poziv);

    if (odgovor.statusCode == 401) {
      await _zavrsiSesiju();
    }

    if (odgovor.statusCode >= 400) {
      throw _uGresku(odgovor);
    }

    if (odgovor.statusCode == 204 || odgovor.bodyBytes.isEmpty) {
      return null;
    }

    return jsonDecode(utf8.decode(odgovor.bodyBytes));
  }

  /// Token je istekao ili je odjavom ponisten. Nema smisla ga dalje slati, a
  /// aplikacija vraca korisnika na prijavu - i kad je odbijen JSON poziv, i kad je
  /// odbijen dohvat fotografije ili PDF-a.
  Future<void> _zavrsiSesiju() async {
    await _pohrana.obrisi();
    naIstekSesije?.call();
  }

  /// Mrezna greska se pretvara u istu vrstu izuzetka kao i greska sa servera,
  /// da ekran ima jedno mjesto za hvatanje umjesto dva.
  Future<http.Response> _uhvatiMreznu(Future<http.Response> Function() poziv) async {
    try {
      return await poziv();
    } on SocketException {
      throw ApiGreska(
        status: 0,
        poruka: 'Server nije dostupan. Provjerite je li pokrenut i pokušajte ponovo.',
        naslov: 'Nema veze sa serverom',
      );
    } on http.ClientException {
      throw ApiGreska(
        status: 0,
        poruka: 'Veza sa serverom je prekinuta. Pokušajte ponovo.',
        naslov: 'Prekinuta veza',
      );
    }
  }

  ApiGreska _uGresku(http.Response odgovor) {
    String? naslov;
    String? poruka;
    final greske = <String, List<String>>{};

    try {
      final tijelo = jsonDecode(utf8.decode(odgovor.bodyBytes));

      if (tijelo is Map<String, dynamic>) {
        naslov = tijelo['title'] as String?;
        poruka = tijelo['detail'] as String?;

        final validacija = tijelo['errors'];

        if (validacija is Map<String, dynamic>) {
          for (final unos in validacija.entries) {
            final vrijednost = unos.value;

            if (vrijednost is List) {
              greske[unos.key] = vrijednost.map((x) => x.toString()).toList();
            }
          }
        }
      }
    } catch (_) {
      // Odgovor nije JSON. To se desava kod gresaka koje ne prolaze kroz filter
      // izuzetaka, pa se ostaje na porukama ispod.
    }

    return ApiGreska(
      status: odgovor.statusCode,
      naslov: naslov,
      poruka: poruka ?? _podrazumijevanaPoruka(odgovor.statusCode, greske),
      greskeValidacije: greske,
    );
  }

  String _podrazumijevanaPoruka(int status, Map<String, List<String>> greske) {
    if (greske.isNotEmpty) {
      return greske.values.first.first;
    }

    switch (status) {
      case 401:
        return 'Sesija je istekla. Prijavite se ponovo.';
      case 403:
        return 'Nemate ovlaštenje za ovu radnju.';
      case 404:
        return 'Traženi zapis ne postoji.';
      default:
        return 'Došlo je do greške. Pokušajte ponovo.';
    }
  }
}

/// Jedan fajl koji ide uz formu.
class FajlZaSlanje {
  const FajlZaSlanje({required this.ime, required this.sadrzaj});

  final String ime;
  final List<int> sadrzaj;
}
