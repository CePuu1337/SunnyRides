import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Tarifa koja vazi za jedan model u zadatom periodu, sa pragovima popusta.
class Cjenovnik {
  const Cjenovnik({
    required this.naziv,
    required this.mnozilac,
    required this.popustPrag1,
    required this.popustProcenat1,
    required this.popustPrag2,
    required this.popustProcenat2,
    this.satnaTarifa,
    this.dnevnaTarifa,
  });

  final String naziv;
  final double mnozilac;
  final double? satnaTarifa;
  final double? dnevnaTarifa;

  final int popustPrag1;
  final double popustProcenat1;
  final int popustPrag2;
  final double popustProcenat2;

  factory Cjenovnik.izJsona(Map<String, dynamic> json) {
    return Cjenovnik(
      naziv: json['naziv']?.toString() ?? '',
      mnozilac: citajDouble(json['mnozilac'], podrazumijevano: 1),
      satnaTarifa: citajDoubleIliNista(json['satnaTarifa']),
      dnevnaTarifa: citajDoubleIliNista(json['dnevnaTarifa']),
      popustPrag1: citajInt(json['popustPrag1']),
      popustProcenat1: citajDouble(json['popustProcenat1']),
      popustPrag2: citajInt(json['popustPrag2']),
      popustProcenat2: citajDouble(json['popustProcenat2']),
    );
  }
}

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

  Future<Cjenovnik?> vazeciCjenovnik(
    int modelVozilaId, {
    DateTime? datum,
  }) async {
    final odgovor = await _klijent.get(
      '/api/cjenovnici/vazeci',
      upit: {'modelVozilaId': modelVozilaId, 'datum': datum},
    );

    return odgovor is Map<String, dynamic> ? Cjenovnik.izJsona(odgovor) : null;
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
