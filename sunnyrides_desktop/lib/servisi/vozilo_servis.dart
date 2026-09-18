import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/vozilo.dart';

class VoziloServis {
  const VoziloServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Vozilo>> lista(UpitVozila upit) async {
    final odgovor = await _klijent.get('/api/vozila', upit: upit.uMapu());

    return Strana.izJsona(odgovor as Map<String, dynamic>, Vozilo.izJsona);
  }

  Future<Vozilo> detalji(int id) async {
    final odgovor = await _klijent.get('/api/vozila/$id');

    return Vozilo.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Vozilo> dodaj(Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.post('/api/vozila', tijelo: zahtjev);

    return Vozilo.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Vozilo> izmijeni(int id, Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.put('/api/vozila/$id', tijelo: zahtjev);

    return Vozilo.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Brisanje na serveru znaci deaktivaciju - vozilo sa historijom se ne uklanja.
  Future<void> obrisi(int id) async {
    await _klijent.delete('/api/vozila/$id');
  }

  Future<List<SlikaVozila>> slike(int voziloId) async {
    final odgovor = await _klijent.get('/api/vozila/$voziloId/slike');

    if (odgovor is! List) {
      return const [];
    }

    return odgovor
        .whereType<Map<String, dynamic>>()
        .map(SlikaVozila.izJsona)
        .toList();
  }

  Future<SlikaVozila> dodajSliku(
    int voziloId, {
    required String imeFajla,
    required List<int> sadrzaj,
  }) async {
    final odgovor = await _klijent.posaljiFajl(
      '/api/vozila/$voziloId/slike',
      nazivPolja: 'fajl',
      imeFajla: imeFajla,
      sadrzaj: sadrzaj,
    );

    return SlikaVozila.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<void> postaviGlavnu(int voziloId, int slikaId) async {
    await _klijent.put('/api/vozila/$voziloId/slike/$slikaId/glavna');
  }

  Future<void> obrisiSliku(int voziloId, int slikaId) async {
    await _klijent.delete('/api/vozila/$voziloId/slike/$slikaId');
  }
}
