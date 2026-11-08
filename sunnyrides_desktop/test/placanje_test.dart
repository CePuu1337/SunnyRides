import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';
import 'package:sunnyrides_desktop/modeli/placanje.dart';

Map<String, dynamic> _placanje(List<int> statusiPovrata) => {
  'id': 7,
  'rezervacijaId': 3,
  'rezervacijaBroj': 'SR-2026-00003',
  'klijentImePrezime': 'Amar Hodžić',
  'poslovnicaNaziv': 'SunnyRides Mostar',
  'iznos': 184.75,
  'naplaceniIznos': 184.75,
  'valuta': 'EUR',
  'status': 3,
  'datumKreiranja': '2026-11-06T00:12:00Z',
  'ukupnoVraceno': 0,
  'povrati': [
    for (var i = 0; i < statusiPovrata.length; i++)
      {
        'id': 100 + i,
        'iznos': 150,
        'razlog': 'Povrat depozita',
        'status': statusiPovrata[i],
        'kreiraoKorisnikIme': null,
        'datumKreiranja': '2026-11-07T10:00:00Z',
      },
  ],
};

void main() {
  test('Ponoviti se može samo povrat koji je Stripe odbio', () {
    final placanje = Placanje.izJsona(_placanje([1, 2, 3, 4, 5]));

    expect(
      placanje.povrati.where((x) => x.mozeSePonoviti).map((x) => x.status),
      [StatusPlacanja.neuspjesno],
    );
    expect(placanje.imaNeuspjeliPovrat, isTrue);
  });

  test('Plaćanje bez odbijenog povrata nema šta ponoviti', () {
    final placanje = Placanje.izJsona(_placanje([3]));

    expect(placanje.imaNeuspjeliPovrat, isFalse);
    expect(placanje.status, StatusPlacanja.uspjesno);
    expect(placanje.povrati.single.kreiraoKorisnikIme, isNull);
  });
}
