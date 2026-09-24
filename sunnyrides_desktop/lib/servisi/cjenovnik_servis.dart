import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/cjenovnik.dart';

class CjenovnikServis {
  const CjenovnikServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Cjenovnik>> lista({
    String? naziv,
    int? modelVozilaId,
    DateTime? vaziNaDatum,
    int stranica = 0,
    int velicinaStranice = 15,
  }) async {
    final odgovor = await _klijent.get(
      '/api/cjenovnici',
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'orderBy': '-datumOd',
        'naziv': naziv,
        'modelVozilaId': modelVozilaId,
        'vaziNaDatum': vaziNaDatum,
      },
    );

    return Strana.izJsona(odgovor as Map<String, dynamic>, Cjenovnik.izJsona);
  }

  Future<void> dodaj(Map<String, dynamic> zahtjev) async {
    await _klijent.post('/api/cjenovnici', tijelo: zahtjev);
  }

  Future<void> izmijeni(int id, Map<String, dynamic> zahtjev) async {
    await _klijent.put('/api/cjenovnici/$id', tijelo: zahtjev);
  }

  Future<void> obrisi(int id) async {
    await _klijent.delete('/api/cjenovnici/$id');
  }
}
