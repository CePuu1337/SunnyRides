/// Adresa API-ja se ne upisuje u kod nego stize kroz --dart-define pri pokretanju.
///
/// Desktop i emulator ne vide isti host: desktop zove localhost, a Android emulator
/// istu masinu vidi kao 10.0.2.2. Zato svaka aplikacija zada svoju podrazumijevanu
/// vrijednost, a --dart-define je nadjacava kad se pusta na stvarnom uredjaju.
class Okruzenje {
  const Okruzenje({required this.osnovnaAdresa});

  /// Korijen API-ja, bez zavrsne kose crte. Primjer: http://localhost:5000
  final String osnovnaAdresa;

  factory Okruzenje.izDefinicija({required String podrazumijevanaAdresa}) {
    const zadano = String.fromEnvironment('API_BASE_URL');

    final adresa = zadano.isEmpty ? podrazumijevanaAdresa : zadano;

    return Okruzenje(osnovnaAdresa: _bezZavrsneCrte(adresa));
  }

  /// Puna adresa slike koju API vraca kao relativnu putanju.
  ///
  /// Fotografije vozila i obavijesti se posluzuju kao obicni staticki fajlovi, pa im
  /// treba samo prefiks. Ako je putanja vec apsolutna, vraca se nepromijenjena.
  String? apsolutnaSlika(String? putanja) {
    if (putanja == null || putanja.isEmpty) {
      return null;
    }

    if (putanja.startsWith('http://') || putanja.startsWith('https://')) {
      return putanja;
    }

    final relativna = putanja.startsWith('/') ? putanja.substring(1) : putanja;

    return '$osnovnaAdresa/$relativna';
  }

  static String _bezZavrsneCrte(String adresa) {
    return adresa.endsWith('/') ? adresa.substring(0, adresa.length - 1) : adresa;
  }
}
