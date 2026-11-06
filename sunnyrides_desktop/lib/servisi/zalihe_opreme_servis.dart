import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/zaliha_opreme.dart';

/// Zalihe opreme po poslovnici. Uposleniku server vraća i dozvoljava samo
/// njegovu poslovnicu.
class ZaliheOpremeServis {
  const ZaliheOpremeServis(this._klijent);

  final ApiKlijent _klijent;

  static const _putanja = '/api/stanja-opreme';

  Future<Strana<ZalihaOpreme>> lista({
    String? naziv,
    int? poslovnicaId,
    int stranica = 0,
    int velicinaStranice = 15,
  }) async {
    final odgovor = await _klijent.get(
      _putanja,
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'naziv': naziv,
        'poslovnicaId': poslovnicaId,
      },
    );

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      ZalihaOpreme.izJsona,
    );
  }

  Future<void> dodaj({
    required int vrstaOpremeId,
    required int poslovnicaId,
    required int kolicina,
  }) async {
    await _klijent.post(
      _putanja,
      tijelo: {
        'vrstaOpremeId': vrstaOpremeId,
        'poslovnicaId': poslovnicaId,
        'kolicina': kolicina,
      },
    );
  }

  Future<void> izmijeni(int id, int kolicina) async {
    await _klijent.put('$_putanja/$id', tijelo: {'kolicina': kolicina});
  }

  Future<void> obrisi(int id) async {
    await _klijent.delete('$_putanja/$id');
  }
}
