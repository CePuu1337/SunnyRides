import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Token se cuva u sistemskoj pohrani tajni, ne u obicnim postavkama.
///
/// Na Windowsu to je DPAPI, na Androidu keystore. Razlog je jednostavan: token je
/// kljuc od naloga dok ne istekne, a SharedPreferences je obican fajl koji svako
/// ko dodje do uredjaja moze procitati.
class PohranaTokena {
  PohranaTokena({FlutterSecureStorage? pohrana})
      : _pohrana = pohrana ?? const FlutterSecureStorage();

  final FlutterSecureStorage _pohrana;

  static const _kljucTokena = 'sunnyrides.token';
  static const _kljucIsteka = 'sunnyrides.istice';

  /// Kopija u memoriji, da se svaki zahtjev ne vraca na disk.
  String? _uMemoriji;
  DateTime? _isticeUMemoriji;

  String? get token => _uMemoriji;

  /// Je li token upotrebljiv. Istekao token se ne salje - server bi ga ionako odbio.
  bool get imaVazeciToken {
    final istice = _isticeUMemoriji;

    if (_uMemoriji == null || _uMemoriji!.isEmpty || istice == null) {
      return false;
    }

    return istice.isAfter(DateTime.now().toUtc());
  }

  Future<void> ucitaj() async {
    _uMemoriji = await _pohrana.read(key: _kljucTokena);

    final istice = await _pohrana.read(key: _kljucIsteka);
    _isticeUMemoriji = istice == null ? null : DateTime.tryParse(istice)?.toUtc();
  }

  Future<void> sacuvaj(String token, DateTime isticeUtc) async {
    _uMemoriji = token;
    _isticeUMemoriji = isticeUtc.toUtc();

    await _pohrana.write(key: _kljucTokena, value: token);
    await _pohrana.write(key: _kljucIsteka, value: isticeUtc.toUtc().toIso8601String());
  }

  Future<void> obrisi() async {
    _uMemoriji = null;
    _isticeUMemoriji = null;

    await _pohrana.delete(key: _kljucTokena);
    await _pohrana.delete(key: _kljucIsteka);
  }
}
