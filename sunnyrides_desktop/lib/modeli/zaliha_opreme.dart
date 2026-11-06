import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Koliko komada jedne vrste opreme poslovnica ima.
class ZalihaOpreme {
  const ZalihaOpreme({
    required this.id,
    required this.vrstaOpremeId,
    required this.vrstaOpremeNaziv,
    required this.poslovnicaId,
    required this.poslovnicaNaziv,
    required this.kolicina,
  });

  final int id;
  final int vrstaOpremeId;
  final String vrstaOpremeNaziv;
  final int poslovnicaId;
  final String poslovnicaNaziv;
  final int kolicina;

  factory ZalihaOpreme.izJsona(Map<String, dynamic> json) {
    return ZalihaOpreme(
      id: citajInt(json['id']),
      vrstaOpremeId: citajInt(json['vrstaOpremeId']),
      vrstaOpremeNaziv: json['vrstaOpremeNaziv']?.toString() ?? '',
      poslovnicaId: citajInt(json['poslovnicaId']),
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString() ?? '',
      kolicina: citajInt(json['kolicina']),
    );
  }
}
