import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

void main() {
  group('Korisnik', () {
    Korisnik napravi(List<String> uloge) {
      return Korisnik.izJsona({
        'id': 1,
        'korisnickoIme': 'administrator',
        'ime': 'Ammar',
        'prezime': 'Puce',
        'email': 'administrator@sunnyrides.example',
        'datumRodjenja': '2003-01-01T00:00:00Z',
        'datumRegistracije': '2026-01-01T00:00:00Z',
        'aktivan': true,
        'blokiran': false,
        'uloge': uloge,
      });
    }

    test('administrator i uposlenik se racunaju kao osoblje', () {
      expect(napravi(['Administrator']).jeOsoblje, isTrue);
      expect(napravi(['Uposlenik']).jeOsoblje, isTrue);
    });

    test('klijent nije osoblje, pa ne smije u desktop aplikaciju', () {
      expect(napravi(['Klijent']).jeOsoblje, isFalse);
    });

    test('inicijali se sastavljaju iz imena i prezimena', () {
      expect(napravi(['Administrator']).inicijali, 'AP');
    });
  });

  group('StatusnaPilula', () {
    testWidgets('prikazuje naziv statusa, ne samo boju', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatusnaPilula.rezervacija(StatusRezervacije.potvrdjena),
          ),
        ),
      );

      expect(find.text('Potvrđena'), findsOneWidget);
    });

    test('nepoznat status ne obara pretvaranje', () {
      expect(StatusRezervacije.izBroja(99), isNull);
      expect(StatusRezervacije.izBroja(2), StatusRezervacije.potvrdjena);
    });
  });
}
