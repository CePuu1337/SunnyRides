import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Ocjena i komentar nakon zavrsenog najma.
class Recenzija {
  const Recenzija({
    required this.id,
    required this.korisnikId,
    required this.voziloId,
    required this.rezervacijaId,
    required this.ocjena,
    required this.datumKreiranja,
    required this.skrivena,
    this.korisnikImePrezime,
    this.voziloOpis,
    this.registarskaOznaka,
    this.rezervacijaBroj,
    this.komentar,
  });

  final int id;
  final int korisnikId;
  final String? korisnikImePrezime;
  final int voziloId;
  final String? voziloOpis;
  final String? registarskaOznaka;
  final int rezervacijaId;
  final String? rezervacijaBroj;
  final int ocjena;
  final String? komentar;
  final DateTime datumKreiranja;

  /// Skrivena recenzija ne ulazi u prosjecnu ocjenu ni u sistem preporuke.
  final bool skrivena;

  factory Recenzija.izJsona(Map<String, dynamic> json) {
    return Recenzija(
      id: citajInt(json['id']),
      korisnikId: citajInt(json['korisnikId']),
      korisnikImePrezime: json['korisnikImePrezime']?.toString(),
      voziloId: citajInt(json['voziloId']),
      voziloOpis: json['voziloOpis']?.toString(),
      registarskaOznaka: json['registarskaOznaka']?.toString(),
      rezervacijaId: citajInt(json['rezervacijaId']),
      rezervacijaBroj: json['rezervacijaBroj']?.toString(),
      ocjena: citajInt(json['ocjena']),
      komentar: json['komentar']?.toString(),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      skrivena: citajBool(json['skrivena']),
    );
  }
}

class UpitRecenzija {
  const UpitRecenzija({
    this.stranica = 0,
    this.velicinaStranice = 15,
    this.klijent,
    this.komentar,
    this.skrivena,
    this.ocjenaDo,
  });

  final int stranica;
  final int velicinaStranice;
  final String? klijent;
  final String? komentar;
  final bool? skrivena;

  /// Gornja granica ocjene. Moderacija pocinje od loših ocjena, pa se filter cesto
  /// postavlja na dvojku ili trojku.
  final int? ocjenaDo;

  UpitRecenzija kopija({
    int? stranica,
    String? klijent,
    String? komentar,
    bool? skrivena,
    int? ocjenaDo,
    bool ocistiSkrivene = false,
    bool ocistiOcjenu = false,
  }) {
    return UpitRecenzija(
      stranica: stranica ?? this.stranica,
      velicinaStranice: velicinaStranice,
      klijent: klijent ?? this.klijent,
      komentar: komentar ?? this.komentar,
      skrivena: ocistiSkrivene ? null : (skrivena ?? this.skrivena),
      ocjenaDo: ocistiOcjenu ? null : (ocjenaDo ?? this.ocjenaDo),
    );
  }

  Map<String, dynamic> uMapu() {
    return {
      'page': stranica,
      'pageSize': velicinaStranice,
      'includeTotalCount': true,
      'orderBy': '-datumKreiranja',
      if (klijent != null && klijent!.isNotEmpty) 'klijent': klijent,
      if (komentar != null && komentar!.isNotEmpty) 'komentar': komentar,
      if (skrivena != null) 'skrivena': skrivena,
      if (ocjenaDo != null) 'ocjenaDo': ocjenaDo,
    };
  }
}

/// Javna objava agencije, vidljiva klijentima u mobilnoj aplikaciji.
class Obavijest {
  const Obavijest({
    required this.id,
    required this.naslov,
    required this.tekst,
    required this.datumObjave,
    required this.aktivna,
    this.slikaUrl,
    this.thumbnailUrl,
  });

  final int id;
  final String naslov;
  final String tekst;
  final String? slikaUrl;
  final String? thumbnailUrl;
  final DateTime datumObjave;
  final bool aktivna;

  /// Datum objave je u buducnosti - obavijest je zakazana i klijenti je jos ne vide.
  bool get zakazana => datumObjave.isAfter(DateTime.now().toUtc());

  /// Klijenti je vide samo ako je aktivna i ako je datum objave prosao.
  bool get vidljivaKlijentima => aktivna && !zakazana;

  factory Obavijest.izJsona(Map<String, dynamic> json) {
    return Obavijest(
      id: citajInt(json['id']),
      naslov: json['naslov']?.toString() ?? '',
      tekst: json['tekst']?.toString() ?? '',
      slikaUrl: json['slikaUrl']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      datumObjave: citajDatum(json['datumObjave']),
      aktivna: citajBool(json['aktivna'], podrazumijevano: true),
    );
  }
}
