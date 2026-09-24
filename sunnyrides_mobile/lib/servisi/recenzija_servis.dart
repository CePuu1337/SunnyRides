import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/recenzija.dart';

class RecenzijaServis {
  const RecenzijaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Recenzija>> zaVozilo(int voziloId, {int koliko = 20}) async {
    final odgovor = await _klijent.get(
      '/api/recenzije',
      upit: {
        'voziloId': voziloId,
        'page': 0,
        'pageSize': koliko,
        'includeTotalCount': true,
        'orderBy': 'DatumKreiranja desc',
      },
    );

    return _strana(odgovor);
  }

  Future<Strana<Recenzija>> moje({int koliko = 50}) async {
    final odgovor = await _klijent.get(
      '/api/recenzije',
      upit: {
        'samoMoje': true,
        'page': 0,
        'pageSize': koliko,
        'includeTotalCount': false,
        'orderBy': 'DatumKreiranja desc',
      },
    );

    return _strana(odgovor);
  }

  Future<List<RezervacijaZaRecenziju>> zaOcjenjivanje() async {
    final odgovor = await _klijent.get('/api/recenzije/za-ocjenjivanje');

    return citajListu(odgovor, RezervacijaZaRecenziju.izJsona);
  }

  Future<Recenzija> ostavi({
    required int rezervacijaId,
    required int ocjena,
    String? komentar,
  }) async {
    final odgovor = await _klijent.post(
      '/api/recenzije',
      tijelo: {
        'rezervacijaId': rezervacijaId,
        'ocjena': ocjena,
        'komentar': komentar,
      },
    );

    return Recenzija.izJsona(odgovor as Map<String, dynamic>);
  }

  static Strana<Recenzija> _strana(dynamic odgovor) {
    return Strana.izJsona(
      odgovor is Map<String, dynamic> ? odgovor : const {},
      Recenzija.izJsona,
    );
  }
}
