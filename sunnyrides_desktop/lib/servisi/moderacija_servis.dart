import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/recenzija.dart';

/// Recenzije i obavijesti - dva manja modula koja osoblje odrzava.
class RecenzijaServis {
  const RecenzijaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Recenzija>> lista(UpitRecenzija upit) async {
    final odgovor = await _klijent.get('/api/recenzije', upit: upit.uMapu());

    return Strana.izJsona(odgovor as Map<String, dynamic>, Recenzija.izJsona);
  }

  /// Skrivanje, ne brisanje: recenzija ostaje zapisana, samo prestaje ulaziti u
  /// prosjecnu ocjenu i u preporuke. Brisanje bi uklonilo trag da je uopste postojala.
  Future<void> sakrij(int id) async {
    await _klijent.post('/api/recenzije/$id/sakrij');
  }

  Future<void> prikazi(int id) async {
    await _klijent.post('/api/recenzije/$id/prikazi');
  }
}

class ObavijestServis {
  const ObavijestServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Obavijest>> lista({
    String? tekst,
    bool? aktivna,
    int stranica = 0,
    int velicinaStranice = 15,
  }) async {
    final odgovor = await _klijent.get(
      '/api/obavijesti',
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'orderBy': '-datumObjave',
        'tekst': tekst,
        'aktivna': aktivna,
      },
    );

    return Strana.izJsona(odgovor as Map<String, dynamic>, Obavijest.izJsona);
  }

  Future<Obavijest> dodaj(Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.post('/api/obavijesti', tijelo: zahtjev);

    return Obavijest.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Obavijest> izmijeni(int id, Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.put('/api/obavijesti/$id', tijelo: zahtjev);

    return Obavijest.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<void> obrisi(int id) async {
    await _klijent.delete('/api/obavijesti/$id');
  }

  Future<Obavijest> postaviSliku(
    int id, {
    required String imeFajla,
    required List<int> sadrzaj,
  }) async {
    final odgovor = await _klijent.posaljiFajl(
      '/api/obavijesti/$id/slika',
      nazivPolja: 'fajl',
      imeFajla: imeFajla,
      sadrzaj: sadrzaj,
    );

    return Obavijest.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<void> ukloniSliku(int id) async {
    await _klijent.delete('/api/obavijesti/$id/slika');
  }
}
