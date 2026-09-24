import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Jedno obavjestenje prijavljenog korisnika.
class Notifikacija {
  const Notifikacija({
    required this.id,
    required this.naslov,
    required this.tekst,
    required this.procitana,
    required this.datumKreiranja,
    this.tip,
    this.rezervacijaId,
    this.rezervacijaBroj,
  });

  final int id;
  final String naslov;
  final String tekst;
  final TipNotifikacije? tip;
  final bool procitana;
  final DateTime datumKreiranja;
  final int? rezervacijaId;
  final String? rezervacijaBroj;

  factory Notifikacija.izJsona(Map<String, dynamic> json) {
    return Notifikacija(
      id: citajInt(json['id']),
      naslov: json['naslov']?.toString() ?? '',
      tekst: json['tekst']?.toString() ?? '',
      tip: TipNotifikacije.izBroja(citajInt(json['tip'])),
      procitana: citajBool(json['procitana']),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      rezervacijaId: json['rezervacijaId'] == null
          ? null
          : citajInt(json['rezervacijaId']),
      rezervacijaBroj: json['rezervacijaBroj']?.toString(),
    );
  }
}
