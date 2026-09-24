import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Ocjena i komentar nakon zavrsenog najma.
class Recenzija {
  const Recenzija({
    required this.id,
    required this.voziloId,
    required this.rezervacijaId,
    required this.ocjena,
    required this.datumKreiranja,
    required this.skrivena,
    this.korisnikImePrezime,
    this.komentar,
    this.voziloOpis,
    this.rezervacijaBroj,
  });

  final int id;
  final int voziloId;
  final int rezervacijaId;
  final int ocjena;
  final String? komentar;
  final DateTime datumKreiranja;
  final bool skrivena;
  final String? korisnikImePrezime;
  final String? voziloOpis;
  final String? rezervacijaBroj;

  factory Recenzija.izJsona(Map<String, dynamic> json) {
    return Recenzija(
      id: citajInt(json['id']),
      voziloId: citajInt(json['voziloId']),
      rezervacijaId: citajInt(json['rezervacijaId']),
      ocjena: citajInt(json['ocjena']),
      komentar: json['komentar']?.toString(),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      skrivena: citajBool(json['skrivena']),
      korisnikImePrezime: json['korisnikImePrezime']?.toString(),
      voziloOpis: json['voziloOpis']?.toString(),
      rezervacijaBroj: json['rezervacijaBroj']?.toString(),
    );
  }
}

/// Zavrsen najam koji jos nije ocijenjen.
class RezervacijaZaRecenziju {
  const RezervacijaZaRecenziju({
    required this.rezervacijaId,
    required this.voziloId,
    required this.broj,
    required this.datumOd,
    required this.datumDo,
    this.voziloOpis,
    this.thumbnailUrl,
  });

  final int rezervacijaId;
  final int voziloId;
  final String broj;
  final DateTime datumOd;
  final DateTime datumDo;
  final String? voziloOpis;
  final String? thumbnailUrl;

  factory RezervacijaZaRecenziju.izJsona(Map<String, dynamic> json) {
    return RezervacijaZaRecenziju(
      rezervacijaId: citajInt(json['rezervacijaId']),
      voziloId: citajInt(json['voziloId']),
      broj: json['broj']?.toString() ?? '',
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      voziloOpis: json['voziloOpis']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
    );
  }
}
