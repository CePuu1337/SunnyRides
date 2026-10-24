import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

// 25.10.2026. je dan jesenje promjene sata u Evropi i ima 25 sati. Testovi ne zavise
// od zone racunara: provjeravaju da se dani broje po kalendaru, sto vazi svugdje.
void main() {
  group('KalendarskiDani', () {
    test('sedmica poslije promjene sata pocinje u ponoc ponedjeljka', () {
      final sljedeca = KalendarskiDani.pomjeri(DateTime(2026, 10, 19), 7);

      expect(sljedeca, DateTime(2026, 10, 26));
      expect(sljedeca.hour, 0);
    });

    test('dan poslije dana od 25 sati je sljedeci datum', () {
      expect(
        KalendarskiDani.pomjeri(DateTime(2026, 10, 25), 1),
        DateTime(2026, 10, 26),
      );
    });

    test('nedjelja pripada sedmici koja je pocela u ponedjeljak prije nje', () {
      expect(
        KalendarskiDani.ponedjeljak(DateTime(2026, 10, 25, 12)),
        DateTime(2026, 10, 19),
      );
    });

    test('prelazi u sljedeci mjesec i godinu', () {
      expect(
        KalendarskiDani.pomjeri(DateTime(2026, 12, 30), 3),
        DateTime(2027, 1, 2),
      );
    });

    test('razlika broji datume, ne sate', () {
      expect(
        KalendarskiDani.razlika(
          DateTime(2026, 10, 19),
          DateTime(2026, 10, 26, 23, 30),
        ),
        7,
      );
    });

    test(
      '09:00 u ponedjeljak poslije promjene sata je na istom mjestu u koloni',
      () {
        final polozaj = KalendarskiDani.polozaj(
          DateTime(2026, 10, 19),
          DateTime(2026, 10, 26, 9),
        );

        expect(polozaj, closeTo(7.375, 0.0001));
      },
    );
  });
}
