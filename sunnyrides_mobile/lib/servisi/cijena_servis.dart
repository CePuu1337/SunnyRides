import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/vozilo.dart';

/// Je li vozilo slobodno u zadatom terminu, i ako nije - zasto.
class Dostupnost {
  const Dostupnost({required this.slobodno, required this.razlog});

  final bool slobodno;
  final String razlog;

  factory Dostupnost.izJsona(Map<String, dynamic> json) {
    return Dostupnost(
      slobodno: citajBool(json['slobodno']),
      razlog: json['razlog']?.toString() ?? '',
    );
  }
}

/// Cijene, oprema, osiguranje i provjera termina.
///
/// Svaki iznos dolazi sa servera. Aplikacija ne racuna ni popust ni ukupno, pa se
/// prikazana i naplacena cijena ne mogu raziici.
class CijenaServis {
  const CijenaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<CijenaRezervacije> obracun(ZahtjevRezervacije zahtjev) async {
    final odgovor = await _klijent.post(
      '/api/cijene/obracun',
      tijelo: zahtjev.uJson(),
    );

    return CijenaRezervacije.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Cijena po satu i po danu i popusti koji vaze za vozilo na dan [naDan] (bez
  /// njega: danas), izracunati na serveru.
  Future<TarifaNaDan> tarifa(int voziloId, {DateTime? naDan}) async {
    final odgovor = await _klijent.get(
      '/api/cijene/vozilo/$voziloId',
      upit: {'naDan': naDan},
    );

    return TarifaNaDan.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Dostupnost> provjeriTermin({
    required int voziloId,
    required DateTime datumOd,
    required DateTime datumDo,
  }) async {
    final odgovor = await _klijent.get(
      '/api/dostupnost/vozila/$voziloId',
      upit: {'datumOd': datumOd, 'datumDo': datumDo},
    );

    return Dostupnost.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<List<VrstaOpreme>> vrsteOpreme() async {
    final odgovor = await _klijent.get(
      '/api/vrste-opreme',
      upit: {
        'page': 0,
        'pageSize': 50,
        'includeTotalCount': false,
        'orderBy': 'Naziv',
      },
    );

    return _lista(odgovor, VrstaOpreme.izJsona);
  }

  Future<List<PaketOsiguranja>> paketiOsiguranja() async {
    final odgovor = await _klijent.get(
      '/api/paketi-osiguranja',
      upit: {
        'page': 0,
        'pageSize': 50,
        'includeTotalCount': false,
        'orderBy': 'CijenaPoDanu',
      },
    );

    return _lista(odgovor, PaketOsiguranja.izJsona);
  }

  static List<T> _lista<T>(
    dynamic odgovor,
    T Function(Map<String, dynamic>) pretvori,
  ) {
    return Strana.izJsona(
      odgovor is Map<String, dynamic> ? odgovor : const {},
      pretvori,
    ).stavke;
  }
}
