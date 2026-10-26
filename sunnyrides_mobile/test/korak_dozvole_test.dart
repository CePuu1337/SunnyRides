import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';
import 'package:sunnyrides_mobile/stanje/sesija.dart';

void main() {
  VozackaDozvola dozvola({
    required bool prednja,
    required bool zadnja,
    int status = 1,
  }) {
    return VozackaDozvola.izJsona({
      'id': 1,
      'korisnikId': 7,
      'brojDozvole': 'BA1234567',
      'datumIzdavanja': '2020-01-01T00:00:00Z',
      'datumIsteka': '2030-01-01T00:00:00Z',
      'status': status,
      'imaPrednjuStranu': prednja,
      'imaZadnjuStranu': zadnja,
      'kategorijaIds': [1],
      'kategorije': ['A'],
    });
  }

  group('Korak vozacke dozvole poslije registracije', () {
    test('bez dozvole korak se mora proci', () {
      expect(Sesija.jePrijavljena(null), isFalse);
    });

    test('podaci bez obje fotografije jos nisu prijava', () {
      expect(
        Sesija.jePrijavljena(dozvola(prednja: true, zadnja: false)),
        isFalse,
      );
    });

    test('podaci i obje fotografije zavrsavaju korak', () {
      expect(
        Sesija.jePrijavljena(dozvola(prednja: true, zadnja: true)),
        isTrue,
      );
    });

    test('odbijena dozvola ne vraca klijenta na korak registracije', () {
      expect(
        Sesija.jePrijavljena(dozvola(prednja: true, zadnja: true, status: 3)),
        isTrue,
      );
    });
  });
}
