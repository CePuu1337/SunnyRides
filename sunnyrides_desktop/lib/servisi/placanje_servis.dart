import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/placanje.dart';

/// Plaćanja i povrati. Uposleniku server vraća samo plaćanja njegove poslovnice.
class PlacanjeServis {
  const PlacanjeServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Placanje>> lista({
    String? rezervacijaBroj,
    int? rezervacijaId,
    StatusPlacanja? status,
    int? poslovnicaId,
    bool samoNeuspjeliPovrati = false,
    int stranica = 0,
    int velicinaStranice = 15,
  }) async {
    final odgovor = await _klijent.get(
      '/api/placanja',
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'rezervacijaBroj': rezervacijaBroj,
        'rezervacijaId': rezervacijaId,
        'status': status?.vrijednost,
        'poslovnicaId': poslovnicaId,
        'samoNeuspjeliPovrati': samoNeuspjeliPovrati ? true : null,
      },
    );

    return Strana.izJsona(odgovor as Map<String, dynamic>, Placanje.izJsona);
  }

  /// Ponovo šalje povrat koji je Stripe odbio. Server pravi novi zapis povrata sa
  /// novim ključem, a odbijeni ostaje u historiji kakav jeste.
  Future<void> ponoviPovrat(int povratId) async {
    await _klijent.post('/api/placanja/povrati/$povratId/ponovi');
  }
}
