import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';
import 'package:sunnyrides_mobile/modeli/vozilo.dart';

void main() {
  group('Grad klijenta', () {
    Korisnik napravi({required List<String> uloge, int? gradId}) {
      return Korisnik.izJsona({
        'id': 7,
        'korisnickoIme': 'novi.klijent',
        'ime': 'Novi',
        'prezime': 'Klijent',
        'email': 'novi.klijent@example.com',
        'datumRodjenja': '2000-01-01T00:00:00Z',
        'datumRegistracije': '2026-10-14T00:00:00Z',
        'uloge': uloge,
        'gradId': gradId,
        'gradNaziv': gradId == null ? null : 'Sarajevo',
      });
    }

    test('klijent bez grada ga mora odabrati', () {
      expect(napravi(uloge: ['Klijent']).trebaOdabratiGrad, isTrue);
    });

    test('klijent sa gradom ide pravo u aplikaciju', () {
      final korisnik = napravi(uloge: ['Klijent'], gradId: 3);

      expect(korisnik.trebaOdabratiGrad, isFalse);
      expect(korisnik.gradNaziv, 'Sarajevo');
    });

    test('osoblje se za grad ne pita', () {
      expect(napravi(uloge: ['Uposlenik']).trebaOdabratiGrad, isFalse);
    });
  });

  group('Udaljenost', () {
    test('blizu sa decimalom, daleko bez nje', () {
      expect(Formati.udaljenost(4.2), '4,2 km');
      expect(Formati.udaljenost(74.9), '75 km');
    });

    test('najbliza poslovnica zna od cega je mjereno', () {
      final odGrada = NajblizaPoslovnica.izJsona({
        'id': 2,
        'naziv': 'SunnyRides Mostar',
        'adresa': 'Kralja Tomislava 12',
        'gradNaziv': 'Mostar',
        'udaljenostKm': 75.0,
        'poLokaciji': false,
        'polazniGrad': 'Sarajevo',
      });

      expect(odGrada.poLokaciji, isFalse);
      expect(odGrada.polazniGrad, 'Sarajevo');
      expect(odGrada.udaljenostKm, 75.0);
    });

    test('vozilo bez poznatog polazista nema udaljenost', () {
      final vozilo = Vozilo.izJsona({'id': 1, 'registarskaOznaka': 'M-01'});

      expect(vozilo.udaljenostKm, isNull);
    });
  });
}
