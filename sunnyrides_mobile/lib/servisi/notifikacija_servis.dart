import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/notifikacija.dart';

class NotifikacijaServis {
  const NotifikacijaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Notifikacija>> lista(
    OsnovniUpit upit, {
    bool? procitana,
  }) async {
    final odgovor = await _klijent.get(
      '/api/notifikacije',
      upit: {...upit.uMapu(), 'procitana': procitana},
    );

    return Strana.izJsona(
      odgovor is Map<String, dynamic> ? odgovor : const {},
      Notifikacija.izJsona,
    );
  }

  Future<int> oznaciProcitanu(int id) async {
    await _klijent.post('/api/notifikacije/$id/procitaj');

    return brojNeprocitanih();
  }

  Future<int> oznaciSveProcitane() async {
    final odgovor = await _klijent.post('/api/notifikacije/procitaj-sve');

    return _broj(odgovor);
  }

  Future<int> brojNeprocitanih() async {
    final odgovor = await _klijent.get('/api/notifikacije/broj-neprocitanih');

    return _broj(odgovor);
  }

  static int _broj(dynamic odgovor) {
    return odgovor is Map<String, dynamic> ? citajInt(odgovor['broj']) : 0;
  }
}
