import 'enumi.dart';
import 'pretvaranje.dart';

/// Vozacka dozvola klijenta, onako kako je vidi osoblje pri verifikaciji.
class VozackaDozvola {
  const VozackaDozvola({
    required this.id,
    required this.korisnikId,
    required this.brojDozvole,
    required this.datumIzdavanja,
    required this.datumIsteka,
    required this.status,
    required this.datumKreiranja,
    required this.imaPrednjuStranu,
    required this.imaZadnjuStranu,
    required this.istekla,
    required this.kategorije,
    required this.kategorijaIds,
    required this.klijentDatumRodjenja,
    this.razlogOdbijanja,
    this.datumVerifikacije,
    this.verifikovaoKorisnikIme,
    this.klijentImePrezime,
    this.klijentEmail,
  });

  final int id;
  final int korisnikId;
  final String brojDozvole;
  final DateTime datumIzdavanja;
  final DateTime datumIsteka;
  final StatusDozvole? status;
  final String? razlogOdbijanja;
  final DateTime? datumVerifikacije;
  final String? verifikovaoKorisnikIme;
  final DateTime datumKreiranja;

  /// Samo podatak da fotografija postoji. Putanja se nikad ne salje - sadrzaj se
  /// preuzima kroz endpoint koji provjerava ko ga smije vidjeti.
  final bool imaPrednjuStranu;

  /// Zadnja strana nosi kategorije, pa bez nje verifikacija nema sta provjeriti.
  final bool imaZadnjuStranu;

  /// Dozvola se ne moze odobriti dok obje strane nisu prilozene.
  bool get imaObjeStrane => imaPrednjuStranu && imaZadnjuStranu;

  bool imaStranu(StranaDozvole strana) =>
      strana == StranaDozvole.prednja ? imaPrednjuStranu : imaZadnjuStranu;

  /// Rok vazenja je prosao, bez obzira na status verifikacije.
  final bool istekla;

  final List<String> kategorije;
  final List<int> kategorijaIds;

  final String? klijentImePrezime;
  final String? klijentEmail;
  final DateTime klijentDatumRodjenja;

  bool get cekaVerifikaciju => status == StatusDozvole.naCekanju;

  /// Godine klijenta na danasnji dan. Kategorije imaju donju granicu godina, pa je
  /// ovo podatak koji uposlenik gleda uz kategorije na dozvoli.
  int get godine {
    final rodjen = klijentDatumRodjenja.toLocal();
    final danas = DateTime.now();

    var godina = danas.year - rodjen.year;

    if (danas.month < rodjen.month ||
        (danas.month == rodjen.month && danas.day < rodjen.day)) {
      godina -= 1;
    }

    return godina;
  }

  factory VozackaDozvola.izJsona(Map<String, dynamic> json) {
    return VozackaDozvola(
      id: citajInt(json['id']),
      korisnikId: citajInt(json['korisnikId']),
      brojDozvole: json['brojDozvole']?.toString() ?? '',
      datumIzdavanja: citajDatum(json['datumIzdavanja']),
      datumIsteka: citajDatum(json['datumIsteka']),
      status: StatusDozvole.izBroja(citajInt(json['status'])),
      razlogOdbijanja: json['razlogOdbijanja']?.toString(),
      datumVerifikacije: citajDatumIliNista(json['datumVerifikacije']),
      verifikovaoKorisnikIme: json['verifikovaoKorisnikIme']?.toString(),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      imaPrednjuStranu: citajBool(json['imaPrednjuStranu']),
      imaZadnjuStranu: citajBool(json['imaZadnjuStranu']),
      istekla: citajBool(json['istekla']),
      kategorije: citajTekstove(json['kategorije']),
      kategorijaIds: (json['kategorijaIds'] as List<dynamic>? ?? const [])
          .map(citajInt)
          .toList(),
      klijentImePrezime: json['klijentImePrezime']?.toString(),
      klijentEmail: json['klijentEmail']?.toString(),
      klijentDatumRodjenja: citajDatum(json['klijentDatumRodjenja']),
    );
  }
}

/// Sta klijent smije voziti prema svojoj dozvoli, i zasto.
class DozvoljeneKategorije {
  const DozvoljeneKategorije({
    required this.posjedovaneKategorije,
    required this.dozvoljeneKategorije,
    required this.mozeRezervisati,
    required this.obrazlozenje,
  });

  /// Kategorije upisane na dozvoli.
  final List<String> posjedovaneKategorije;

  /// Kategorije vozila koje smije voziti, ukljucujuci pokrivene hijerarhijom.
  final List<String> dozvoljeneKategorije;

  final bool mozeRezervisati;
  final String obrazlozenje;

  /// Kategorije koje dobija kroz hijerarhiju, a nisu upisane na dozvoli.
  List<String> get izvedene => dozvoljeneKategorije
      .where((x) => !posjedovaneKategorije.contains(x))
      .toList();

  factory DozvoljeneKategorije.izJsona(Map<String, dynamic> json) {
    return DozvoljeneKategorije(
      posjedovaneKategorije: citajTekstove(json['posjedovaneKategorije']),
      dozvoljeneKategorije: citajTekstove(json['dozvoljeneKategorije']),
      mozeRezervisati: citajBool(json['mozeRezervisati']),
      obrazlozenje: json['obrazlozenje']?.toString() ?? '',
    );
  }
}
