import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_mobile/ekrani/pretraga/filteri.dart';
import 'package:sunnyrides_mobile/modeli/vozilo.dart';

void main() {
  group('Vozilo', () {
    Vozilo napravi({required bool elektricno, int kubikaza = 125}) {
      return Vozilo.izJsona({
        'id': 1,
        'modelVozilaId': 2,
        'poslovnicaId': 3,
        'registarskaOznaka': 'A12-B-345',
        'godinaProizvodnje': 2024,
        'aktivno': true,
        'satnaTarifa': 8,
        'dnevnaTarifa': 45,
        'iznosDepozita': 100,
        'kubikaza': kubikaza,
        'snagaKw': 4.6,
        'jeElektricno': elektricno,
        'kategorijaDozvoleId': 1,
        'markaNaziv': 'Niu',
        'modelNaziv': 'NQi GT',
      });
    }

    test('vozilo na struju prikazuje snagu, jer kubikazu nema', () {
      expect(napravi(elektricno: true, kubikaza: 0).pogon, '4.6 kW');
    });

    test('vozilo sa motorom prikazuje kubikazu', () {
      expect(napravi(elektricno: false).pogon, '125 ccm');
    });

    test('naziv spaja marku i model', () {
      expect(napravi(elektricno: false).naziv, 'Niu NQi GT');
    });
  });

  group('Filteri', () {
    test('termin vazi samo kad su oba datuma zadata i u ispravnom redu', () {
      final od = DateTime(2026, 7, 1, 9);
      final doDatum = DateTime(2026, 7, 4, 18);

      expect(const Filteri().imaTermin, isFalse);
      expect(Filteri(datumOd: od).imaTermin, isFalse);
      expect(Filteri(datumOd: doDatum, datumDo: od).imaTermin, isFalse);
      expect(Filteri(datumOd: od, datumDo: doDatum).imaTermin, isTrue);
    });

    test(
      'kopija razlikuje neproslijedjeno od praznog, pa se filter moze ukloniti',
      () {
        const polazni = Filteri(tipVozilaId: 5, gradId: 2);

        expect(polazni.kopija(gradId: null).tipVozilaId, 5);
        expect(polazni.kopija(gradId: null).gradId, isNull);
        expect(polazni.kopija(poredak: Poredak.ocjena).gradId, 2);
      },
    );

    test('pretraga pocinje poretkom po preporuci, ne po cijeni', () {
      expect(const Filteri().poredak, Poredak.preporuka);
      expect(Poredak.preporuka.vrijednost, 'Preporuka');
    });

    test('marka, poslovnica i cjenovni raspon se broje kao filteri', () {
      const sa = Filteri(
        markaId: 3,
        poslovnicaId: 7,
        cijenaOd: 20,
        cijenaDo: 60,
      );

      // Raspon je jedan filter, bez obzira koliko granica ima.
      expect(sa.brojAktivnih, 3);
      expect(sa.kopija(cijenaDo: null).brojAktivnih, 3);
      expect(sa.kopija(cijenaOd: null, cijenaDo: null).brojAktivnih, 2);
      expect(sa.kopija(poslovnicaId: null).markaId, 3);
    });

    test('broj aktivnih filtera ne racuna nepotpun termin', () {
      const sa = Filteri(tipVozilaId: 1, cijenaDo: 50);

      expect(sa.brojAktivnih, 2);
      expect(sa.kopija(datumOd: DateTime(2026, 7, 1)).brojAktivnih, 2);
    });
  });

  group('Cijena u filterima', () {
    test('prazno polje znaci bez granice', () {
      expect(ValidacijaCijene.procitaj(''), isNull);
      expect(ValidacijaCijene.iznos('  '), isNull);
    });

    test('zarez i tacka se prihvataju kao decimalni znak', () {
      expect(ValidacijaCijene.procitaj('42,5'), 42.5);
      expect(ValidacijaCijene.procitaj('42.5'), 42.5);
    });

    test('tekst i negativan iznos se odbijaju porukom', () {
      expect(ValidacijaCijene.iznos('abc'), isNotNull);
      expect(ValidacijaCijene.iznos('-5'), isNotNull);
      expect(ValidacijaCijene.iznos('40'), isNull);
    });

    test('gornja granica ne smije biti manja od donje', () {
      expect(ValidacijaCijene.raspon('50', '30'), isNotNull);
      expect(ValidacijaCijene.raspon('30', '50'), isNull);
      expect(ValidacijaCijene.raspon('', '30'), isNull);
    });
  });
}
