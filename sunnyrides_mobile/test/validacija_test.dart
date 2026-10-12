import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

void main() {
  group('Email', () {
    test('mora imati oblik adrese', () {
      expect(Validatori.email(''), isNotNull);
      expect(Validatori.email('ammar'), isNotNull);
      expect(Validatori.email('ammar@primjer'), isNotNull);
      expect(Validatori.email('a@b.'), isNotNull);
      expect(Validatori.email('ammar puce@primjer.com'), isNotNull);
    });

    test('razmaci oko adrese se zanemaruju', () {
      expect(Validatori.email(' ammar@primjer.com '), isNull);
    });

    test('poruka kaze kakav se oblik ocekuje', () {
      expect(Validatori.email('ammar'), contains('ime@primjer.com'));
    });
  });

  group('Telefon', () {
    test('prihvata samo oblik koji trazi i server', () {
      expect(Validatori.telefon('+387 61 234 567'), isNull);
      expect(Validatori.telefon('061 234 567'), isNotNull);
      expect(Validatori.telefon('+387 33 234 567'), isNotNull);
      expect(Validatori.telefon('+38761234567'), isNotNull);
    });

    test('prazan je greska samo kad je polje obavezno', () {
      expect(Validatori.telefon(''), isNotNull);
      expect(Validatori.telefon('', obavezan: false), isNull);
      expect(Validatori.telefon('061', obavezan: false), isNotNull);
    });
  });

  group('Lozinka', () {
    test('ima iste granice kao na serveru', () {
      expect(Validatori.lozinka('12345'), isNotNull);
      expect(Validatori.lozinka('123456'), isNull);
      expect(Validatori.lozinka('a' * 100), isNull);
      expect(Validatori.lozinka('a' * 101), isNotNull);
    });

    test('potvrda mora biti ista kao lozinka', () {
      expect(Validatori.potvrdaLozinke('tajna1', 'tajna1'), isNull);
      expect(Validatori.potvrdaLozinke('tajna2', 'tajna1'), isNotNull);
    });
  });

  group('Korisnicko ime', () {
    test('duzina od 3 do 50 znakova', () {
      expect(Validatori.korisnickoIme('ab'), isNotNull);
      expect(Validatori.korisnickoIme('abc'), isNull);
      expect(Validatori.korisnickoIme('a' * 51), isNotNull);
    });

    test('bez razmaka i slova sa kvacicama', () {
      expect(Validatori.korisnickoIme('ammar.puce_2'), isNull);
      expect(Validatori.korisnickoIme('ammar puce'), isNotNull);
      expect(Validatori.korisnickoIme('čedo'), isNotNull);
    });
  });
}
