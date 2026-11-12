import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';
import 'package:sunnyrides_desktop/ekrani/kalendar/pogodjene_rezervacije.dart';
import 'package:sunnyrides_desktop/modeli/kalendar.dart';

PogodjenaRezervacija _rezervacija(DateTime od) => PogodjenaRezervacija(
  id: 1,
  broj: 'SR-2026-00001',
  datumOd: od,
  datumDo: od.add(const Duration(days: 2)),
  status: StatusRezervacije.potvrdjena,
  isPaid: true,
);

void main() {
  final trazeniKraj = DateTime(2026, 11, 20, 18);

  test('Bez pogođenih rezervacija blokada traje koliko je zadano', () {
    expect(
      krajBlokade(doDatuma: trazeniKraj, pogodjene: const [], skrati: true),
      trazeniKraj,
    );
  });

  test('Skraćena blokada staje na početku prve pogođene rezervacije', () {
    final prva = DateTime(2026, 11, 17, 9);
    final druga = DateTime(2026, 11, 19, 9);

    expect(
      krajBlokade(
        doDatuma: trazeniKraj,
        pogodjene: [_rezervacija(druga), _rezervacija(prva)],
        skrati: true,
      ),
      prva,
    );
  });

  test('Bez skraćivanja blokada ostaje do zadanog kraja', () {
    expect(
      krajBlokade(
        doDatuma: trazeniKraj,
        pogodjene: [_rezervacija(DateTime(2026, 11, 17, 9))],
        skrati: false,
      ),
      trazeniKraj,
    );
  });
}
