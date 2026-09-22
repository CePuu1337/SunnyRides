import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/kalendar.dart';

class KalendarServis {
  const KalendarServis(this._klijent);

  final ApiKlijent _klijent;

  Future<KalendarFlote> zauzetost({
    required DateTime od,
    required DateTime doDatuma,
    int? poslovnicaId,
    int? tipVozilaId,
    String? vozilo,
  }) async {
    final odgovor = await _klijent.get(
      '/api/kalendar-flote',
      upit: {
        'od': od,
        'do': doDatuma,
        'poslovnicaId': poslovnicaId,
        'tipVozilaId': tipVozilaId,
        'vozilo': vozilo,
      },
    );

    return KalendarFlote.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<List<KlijentZaOdabir>> klijenti(String tekst) async {
    final odgovor = await _klijent.get(
      '/api/korisnici/klijenti',
      upit: {
        'tekst': tekst,
        'page': 0,
        'pageSize': 15,
        'includeTotalCount': false,
      },
    );

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      KlijentZaOdabir.izJsona,
    ).stavke;
  }

  Future<List<VrstaOpreme>> vrsteOpreme() async {
    final odgovor = await _klijent.get(
      '/api/vrste-opreme',
      upit: {'page': 0, 'pageSize': 100, 'includeTotalCount': false},
    );

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      VrstaOpreme.izJsona,
    ).stavke;
  }

  Future<List<PaketOsiguranja>> paketiOsiguranja() async {
    final odgovor = await _klijent.get(
      '/api/paketi-osiguranja',
      upit: {'page': 0, 'pageSize': 100, 'includeTotalCount': false},
    );

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      PaketOsiguranja.izJsona,
    ).stavke;
  }

  Future<CijenaRezervacije> obracun(ZahtjevRezervacije zahtjev) async {
    final odgovor = await _klijent.post(
      '/api/cijene/obracun',
      tijelo: zahtjev.uJson(),
    );

    return CijenaRezervacije.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<void> kreirajZaKlijenta(
    int klijentId,
    ZahtjevRezervacije zahtjev,
  ) async {
    await _klijent.post(
      '/api/rezervacije/klijent/$klijentId',
      tijelo: zahtjev.uJson(),
    );
  }

  /// Rezervacije koje bi planirana blokada pogodila. Nista ne mijenja.
  Future<List<PogodjenaRezervacija>> pogodjeneRezervacije({
    required int voziloId,
    required DateTime od,
    required DateTime doDatuma,
  }) async {
    final odgovor = await _klijent.get(
      '/api/dostupnost/pogodjene-rezervacije',
      upit: {'voziloId': voziloId, 'datumOd': od, 'datumDo': doDatuma},
    );

    if (odgovor is! List) {
      return const [];
    }

    return odgovor
        .whereType<Map<String, dynamic>>()
        .map(PogodjenaRezervacija.izJsona)
        .toList();
  }

  /// Blokada vozila. Server je ne odbija ni kad preklapa rezervaciju - vozilo se
  /// pokvari bez obzira na to sto je iznajmljeno - pa je na formi da to pokaze.
  Future<void> blokirajVozilo({
    required int voziloId,
    required DateTime od,
    required DateTime doDatuma,
    required String razlog,
  }) async {
    await _klijent.post(
      '/api/blokade',
      tijelo: {
        'voziloId': voziloId,
        'datumOd': od.toUtc().toIso8601String(),
        'datumDo': doDatuma.toUtc().toIso8601String(),
        'razlog': razlog,
      },
    );
  }
}
