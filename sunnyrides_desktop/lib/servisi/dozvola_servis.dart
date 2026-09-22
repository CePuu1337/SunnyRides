import 'dart:typed_data';

import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/dozvola.dart';

class DozvolaServis {
  const DozvolaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<VozackaDozvola>> lista(UpitDozvola upit) async {
    final odgovor = await _klijent.get('/api/dozvole', upit: upit.uMapu());

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      VozackaDozvola.izJsona,
    );
  }

  Future<VozackaDozvola> detalji(int id) async {
    final odgovor = await _klijent.get('/api/dozvole/$id');

    return VozackaDozvola.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Fotografija dozvole. Ide kroz API klijent sa tokenom, ne kao staticki fajl -
  /// to je licni dokument, a ne katalog vozila.
  Future<Uint8List> fotografija(int id, StranaDozvole strana) {
    return _klijent.bajtovi(
      '/api/dozvole/$id/fotografija',
      upit: {'strana': strana.vrijednost},
    );
  }

  Future<DozvoljeneKategorije> dozvoljeneKategorije(int id) async {
    final odgovor = await _klijent.get(
      '/api/dozvole/$id/dozvoljene-kategorije',
    );

    return DozvoljeneKategorije.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<VozackaDozvola> odobri(int id) async {
    final odgovor = await _klijent.post('/api/dozvole/$id/odobri');

    return VozackaDozvola.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<VozackaDozvola> odbij(int id, String razlog) async {
    final odgovor = await _klijent.post(
      '/api/dozvole/$id/odbij',
      tijelo: {'razlog': razlog},
    );

    return VozackaDozvola.izJsona(odgovor as Map<String, dynamic>);
  }
}
