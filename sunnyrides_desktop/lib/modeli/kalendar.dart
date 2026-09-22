import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Zauzetost flote kroz jedan period, red po vozilu.
class KalendarFlote {
  const KalendarFlote({
    required this.od,
    required this.doDatuma,
    required this.bufferSati,
    required this.vozila,
  });

  final DateTime od;
  final DateTime doDatuma;

  /// Sati pripreme izmedju dva najma. Crta se kao blijedi razmak iza bloka.
  final double bufferSati;

  final List<RedKalendara> vozila;

  factory KalendarFlote.izJsona(Map<String, dynamic> json) {
    return KalendarFlote(
      od: citajDatum(json['od']),
      doDatuma: citajDatum(json['do']),
      bufferSati: citajDouble(json['bufferSati']),
      vozila: citajListu(json['vozila'], RedKalendara.izJsona),
    );
  }
}

class RedKalendara {
  const RedKalendara({
    required this.voziloId,
    required this.vozilo,
    required this.registarskaOznaka,
    required this.tipVozila,
    required this.poslovnica,
    required this.blokovi,
    this.thumbnailUrl,
  });

  final int voziloId;
  final String vozilo;
  final String registarskaOznaka;
  final String tipVozila;
  final String poslovnica;
  final String? thumbnailUrl;
  final List<BlokKalendara> blokovi;

  factory RedKalendara.izJsona(Map<String, dynamic> json) {
    return RedKalendara(
      voziloId: citajInt(json['voziloId']),
      vozilo: json['vozilo']?.toString() ?? '',
      registarskaOznaka: json['registarskaOznaka']?.toString() ?? '',
      tipVozila: json['tipVozila']?.toString() ?? '',
      poslovnica: json['poslovnica']?.toString() ?? '',
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      blokovi: citajListu(json['blokovi'], BlokKalendara.izJsona),
    );
  }
}

/// Jedan zauzet raspon - rezervacija ili blokada vozila.
class BlokKalendara {
  const BlokKalendara({
    required this.vrsta,
    required this.od,
    required this.doDatuma,
    this.rezervacijaId,
    this.broj,
    this.klijent,
    this.status,
    this.drziDo,
    this.razlog,
  });

  final VrstaBlokaKalendara? vrsta;
  final DateTime od;
  final DateTime doDatuma;

  final int? rezervacijaId;
  final String? broj;
  final String? klijent;
  final StatusRezervacije? status;
  final DateTime? drziDo;

  final String? razlog;

  bool get jeBlokada => vrsta == VrstaBlokaKalendara.blokada;

  factory BlokKalendara.izJsona(Map<String, dynamic> json) {
    return BlokKalendara(
      vrsta: VrstaBlokaKalendara.izBroja(citajInt(json['vrsta'])),
      od: citajDatum(json['od']),
      doDatuma: citajDatum(json['do']),
      rezervacijaId: json['rezervacijaId'] == null
          ? null
          : citajInt(json['rezervacijaId']),
      broj: json['broj']?.toString(),
      klijent: json['klijent']?.toString(),
      status: json['status'] == null
          ? null
          : StatusRezervacije.izBroja(citajInt(json['status'])),
      drziDo: citajDatumIliNista(json['drziDo']),
      razlog: json['razlog']?.toString(),
    );
  }
}

/// Rezervacija koju bi planirana blokada pogodila.
///
/// Nosi i kontakt klijenta, jer uposlenik koji blokira vozilo zbog kvara mora znati
/// koga treba nazvati - blokada sama po sebi nikoga ne obavjestava.
class PogodjenaRezervacija {
  const PogodjenaRezervacija({
    required this.id,
    required this.broj,
    required this.datumOd,
    required this.datumDo,
    required this.status,
    required this.isPaid,
    this.klijentImePrezime,
    this.klijentEmail,
    this.klijentTelefon,
  });

  final int id;
  final String broj;
  final DateTime datumOd;
  final DateTime datumDo;
  final StatusRezervacije? status;
  final bool isPaid;
  final String? klijentImePrezime;
  final String? klijentEmail;
  final String? klijentTelefon;

  factory PogodjenaRezervacija.izJsona(Map<String, dynamic> json) {
    return PogodjenaRezervacija(
      id: citajInt(json['id']),
      broj: json['broj']?.toString() ?? '',
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      status: StatusRezervacije.izBroja(citajInt(json['status'])),
      isPaid: citajBool(json['isPaid']),
      klijentImePrezime: json['klijentImePrezime']?.toString(),
      klijentEmail: json['klijentEmail']?.toString(),
      klijentTelefon: json['klijentTelefon']?.toString(),
    );
  }
}

/// Klijent u padajucoj listi pri rucnom unosu rezervacije.
class KlijentZaOdabir {
  const KlijentZaOdabir({
    required this.id,
    required this.ime,
    required this.prezime,
    required this.email,
    required this.blokiran,
    this.telefon,
    this.statusDozvole,
  });

  final int id;
  final String ime;
  final String prezime;
  final String email;
  final String? telefon;
  final bool blokiran;
  final StatusDozvole? statusDozvole;

  String get punoIme => '$ime $prezime';

  /// Sta ce zaustaviti rezervaciju, ako ista. Prazno kad klijent smije rezervisati.
  String? get prepreka {
    if (blokiran) {
      return 'Nalog je blokiran.';
    }

    switch (statusDozvole) {
      case StatusDozvole.odobrena:
        return null;
      case StatusDozvole.naCekanju:
        return 'Dozvola još čeka verifikaciju.';
      case StatusDozvole.odbijena:
        return 'Dozvola je odbijena.';
      case null:
        return 'Klijent nije predao vozačku dozvolu.';
    }
  }

  factory KlijentZaOdabir.izJsona(Map<String, dynamic> json) {
    return KlijentZaOdabir(
      id: citajInt(json['id']),
      ime: json['ime']?.toString() ?? '',
      prezime: json['prezime']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      telefon: json['telefon']?.toString(),
      blokiran: citajBool(json['blokiran']),
      statusDozvole: json['statusDozvole'] == null
          ? null
          : StatusDozvole.izBroja(citajInt(json['statusDozvole'])),
    );
  }
}
