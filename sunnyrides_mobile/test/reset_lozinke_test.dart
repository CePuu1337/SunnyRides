import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_mobile/ekrani/prijava/zaboravljena_lozinka_ekran.dart';

void main() {
  group('Reset lozinke', () {
    test('email mora imati oblik adrese', () {
      expect(ValidacijaResetaLozinke.email(''), isNotNull);
      expect(ValidacijaResetaLozinke.email('ammar'), isNotNull);
      expect(ValidacijaResetaLozinke.email('ammar@primjer'), isNotNull);
      expect(ValidacijaResetaLozinke.email(' ammar@primjer.com '), isNull);
    });

    test('kod se prihvata sa malim slovima, razmacima i crticom', () {
      expect(ValidacijaResetaLozinke.kod('K7M2Q9XA'), isNull);
      expect(ValidacijaResetaLozinke.kod('k7m2 q9xa'), isNull);
      expect(ValidacijaResetaLozinke.kod('K7M2-Q9XA'), isNull);
    });

    test(
      'kod pogresne duzine ili sa znakovima kojih u kodu nema se odbija',
      () {
        expect(ValidacijaResetaLozinke.kod(''), isNotNull);
        expect(ValidacijaResetaLozinke.kod('K7M2Q9X'), isNotNull);
        expect(ValidacijaResetaLozinke.kod('K7M2Q9XAB'), isNotNull);

        // O, I, 0 i 1 server nikad ne generise.
        expect(ValidacijaResetaLozinke.kod('K7M2Q9XO'), isNotNull);
        expect(ValidacijaResetaLozinke.kod('K7M2Q9X1'), isNotNull);
      },
    );

    test('nova lozinka ima isti minimum kao na serveru', () {
      expect(ValidacijaResetaLozinke.lozinka('12345'), isNotNull);
      expect(ValidacijaResetaLozinke.lozinka('123456'), isNull);
    });
  });
}
