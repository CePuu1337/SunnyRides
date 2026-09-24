import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/vozilo.dart';

/// Sve sto klijent cita iz ponude: preporuke, vozila, obavijesti i sifarnici koji
/// popunjavaju filtere.
class KatalogServis {
  const KatalogServis(this._klijent);

  final ApiKlijent _klijent;

  Future<List<Preporuka>> preporuke({
    DateTime? slobodnoOd,
    DateTime? slobodnoDo,
    int? tipVozilaId,
    int? gradId,
    int koliko = 6,
  }) async {
    final odgovor = await _klijent.get(
      '/api/preporuke',
      upit: {
        'slobodnoOd': slobodnoOd,
        'slobodnoDo': slobodnoDo,
        'tipVozilaId': tipVozilaId,
        'gradId': gradId,
        'page': 0,
        'pageSize': koliko,
        'includeTotalCount': false,
      },
    );

    return Strana.izJsona(_mapa(odgovor), Preporuka.izJsona).stavke;
  }

  Future<List<Preporuka>> slicnaVozila(int voziloId, {int koliko = 4}) async {
    final odgovor = await _klijent.get(
      '/api/preporuke/slicna/$voziloId',
      upit: {'broj': koliko},
    );

    return citajListu(odgovor, Preporuka.izJsona);
  }

  Future<Strana<Vozilo>> vozila({
    required OsnovniUpit upit,
    String? modelNaziv,
    int? tipVozilaId,
    int? gradId,
    int? poslovnicaId,
    double? cijenaOd,
    double? cijenaDo,
    DateTime? slobodnoOd,
    DateTime? slobodnoDo,
    bool samoDozvoljenaZaMene = true,
  }) async {
    final odgovor = await _klijent.get(
      '/api/vozila',
      upit: {
        ...upit.uMapu(),
        'modelNaziv': modelNaziv,
        'tipVozilaId': tipVozilaId,
        'gradId': gradId,
        'poslovnicaId': poslovnicaId,
        'cijenaOd': cijenaOd,
        'cijenaDo': cijenaDo,
        'slobodnoOd': slobodnoOd,
        'slobodnoDo': slobodnoDo,
        'aktivno': true,
        'samoDozvoljenaZaMene': samoDozvoljenaZaMene,
      },
    );

    return Strana.izJsona(_mapa(odgovor), Vozilo.izJsona);
  }

  Future<Vozilo> vozilo(int id) async {
    final odgovor = await _klijent.get('/api/vozila/$id');

    return Vozilo.izJsona(_mapa(odgovor));
  }

  Future<List<SlikaVozila>> slike(int voziloId) async {
    final odgovor = await _klijent.get('/api/vozila/$voziloId/slike');

    return citajListu(odgovor, SlikaVozila.izJsona);
  }

  Future<List<Obavijest>> obavijesti({int koliko = 5}) async {
    final odgovor = await _klijent.get(
      '/api/obavijesti',
      upit: {
        'page': 0,
        'pageSize': koliko,
        'includeTotalCount': false,
        'orderBy': 'DatumObjave desc',
      },
    );

    return Strana.izJsona(_mapa(odgovor), Obavijest.izJsona).stavke;
  }

  Future<List<Stavka>> tipoviVozila() => _sifarnik('/api/tipovi-vozila');

  Future<List<Stavka>> gradovi() => _sifarnik('/api/gradovi');

  /// Sta prijavljeni korisnik smije voziti. Stoji iznad rezultata pretrage, da mu
  /// bude jasno zasto pojedina vozila nisu ponudjena.
  Future<DozvoljeneKategorije> mojeKategorije() async {
    final odgovor = await _klijent.get('/api/dozvole/moje-kategorije');

    return DozvoljeneKategorije.izJsona(_mapa(odgovor));
  }

  Future<List<Stavka>> _sifarnik(String putanja) async {
    final odgovor = await _klijent.get(
      putanja,
      upit: {
        'page': 0,
        'pageSize': 100,
        'includeTotalCount': false,
        'orderBy': 'Naziv',
      },
    );

    return Strana.izJsona(_mapa(odgovor), Stavka.izJsona).stavke;
  }

  static Map<String, dynamic> _mapa(dynamic odgovor) {
    return odgovor is Map<String, dynamic> ? odgovor : const {};
  }
}
