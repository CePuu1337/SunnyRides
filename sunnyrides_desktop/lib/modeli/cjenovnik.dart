import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Tarifa koja vazi za jedan model vozila u jednom sezonskom periodu.
///
/// Pragovi popusta su podatak, ne konstanta u kodu: obracun ih cita odavde, pa se
/// sezonska akcija mijenja unosom umjesto ponovnim prevodjenjem aplikacije.
class Cjenovnik {
  const Cjenovnik({
    required this.id,
    required this.modelVozilaId,
    required this.naziv,
    required this.datumOd,
    required this.datumDo,
    required this.mnozilac,
    required this.popustPrag1,
    required this.popustProcenat1,
    required this.popustPrag2,
    required this.popustProcenat2,
    this.satnaTarifa,
    this.dnevnaTarifa,
    this.modelNaziv,
    this.markaNaziv,
  });

  final int id;
  final int modelVozilaId;
  final String naziv;
  final DateTime datumOd;
  final DateTime datumDo;

  /// Sezonski mnozilac. Jedan znaci bez uvecanja i bez umanjenja.
  final double mnozilac;

  /// Prazno znaci da se koristi tarifa upisana na samom vozilu.
  final double? satnaTarifa;
  final double? dnevnaTarifa;

  final int popustPrag1;
  final double popustProcenat1;
  final int popustPrag2;
  final double popustProcenat2;

  final String? modelNaziv;
  final String? markaNaziv;

  String get model => [
    markaNaziv,
    modelNaziv,
  ].where((x) => x != null && x.isNotEmpty).join(' ');

  bool vaziNa(DateTime dan) =>
      !dan.isBefore(datumOd.toLocal()) && !dan.isAfter(datumDo.toLocal());

  factory Cjenovnik.izJsona(Map<String, dynamic> json) {
    return Cjenovnik(
      id: citajInt(json['id']),
      modelVozilaId: citajInt(json['modelVozilaId']),
      naziv: json['naziv']?.toString() ?? '',
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      mnozilac: citajDouble(json['mnozilac'], podrazumijevano: 1),
      satnaTarifa: citajDoubleIliNista(json['satnaTarifa']),
      dnevnaTarifa: citajDoubleIliNista(json['dnevnaTarifa']),
      popustPrag1: citajInt(json['popustPrag1']),
      popustProcenat1: citajDouble(json['popustProcenat1']),
      popustPrag2: citajInt(json['popustPrag2']),
      popustProcenat2: citajDouble(json['popustProcenat2']),
      modelNaziv: json['modelNaziv']?.toString(),
      markaNaziv: json['markaNaziv']?.toString(),
    );
  }
}
