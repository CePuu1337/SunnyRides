import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/pregled_poslovanja.dart';

class PregledServis {
  const PregledServis(this._klijent);

  final ApiKlijent _klijent;

  Future<PregledPoslovanja> pregled() async {
    final odgovor = await _klijent.get('/api/pregled-poslovanja');

    return PregledPoslovanja.izJsona(odgovor as Map<String, dynamic>);
  }
}
