import 'pretvaranje.dart';

/// Nalog kakav API vraca. Uloge stizu kao nazivi, ne kao identifikatori - po njima
/// aplikacija odlucuje sta uopste nudi u meniju.
class Korisnik {
  const Korisnik({
    required this.id,
    required this.korisnickoIme,
    required this.ime,
    required this.prezime,
    required this.email,
    required this.datumRodjenja,
    required this.datumRegistracije,
    required this.aktivan,
    required this.blokiran,
    required this.uloge,
    this.telefon,
    this.putanjaSlike,
    this.thumbnailUrl,
  });

  final int id;
  final String korisnickoIme;
  final String ime;
  final String prezime;
  final String email;
  final String? telefon;
  final DateTime datumRodjenja;
  final DateTime datumRegistracije;
  final String? putanjaSlike;
  final String? thumbnailUrl;
  final bool aktivan;
  final bool blokiran;
  final List<String> uloge;

  String get punoIme => '$ime $prezime';

  /// Inicijali za krug u zaglavlju, kad korisnik nema profilnu sliku.
  String get inicijali {
    final prvo = ime.isEmpty ? '' : ime.substring(0, 1);
    final drugo = prezime.isEmpty ? '' : prezime.substring(0, 1);

    return (prvo + drugo).toUpperCase();
  }

  bool imaUlogu(String uloga) {
    return uloge.any((x) => x.toLowerCase() == uloga.toLowerCase());
  }

  bool get jeAdministrator => imaUlogu(Uloge.administrator);
  bool get jeUposlenik => imaUlogu(Uloge.uposlenik);
  bool get jeKlijent => imaUlogu(Uloge.klijent);

  /// Smije li uci u desktop aplikaciju. Klijent nema sta traziti u njoj i obrnuto.
  bool get jeOsoblje => jeAdministrator || jeUposlenik;

  /// Naziv uloge kakav se prikazuje uz ime u zaglavlju.
  String get glavnaUloga {
    if (jeAdministrator) {
      return 'Administrator';
    }

    if (jeUposlenik) {
      return 'Uposlenik';
    }

    return uloge.isEmpty ? 'Klijent' : uloge.first;
  }

  factory Korisnik.izJsona(Map<String, dynamic> json) {
    return Korisnik(
      id: citajInt(json['id']),
      korisnickoIme: json['korisnickoIme']?.toString() ?? '',
      ime: json['ime']?.toString() ?? '',
      prezime: json['prezime']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      telefon: json['telefon']?.toString(),
      datumRodjenja: citajDatum(json['datumRodjenja']),
      datumRegistracije: citajDatum(json['datumRegistracije']),
      putanjaSlike: json['putanjaSlike']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      aktivan: citajBool(json['aktivan'], podrazumijevano: true),
      blokiran: citajBool(json['blokiran']),
      uloge: citajTekstove(json['uloge']),
    );
  }
}

/// Nazivi uloga onako kako ih server salje.
class Uloge {
  const Uloge._();

  static const administrator = 'Administrator';
  static const uposlenik = 'Uposlenik';
  static const klijent = 'Klijent';
}

/// Odgovor na prijavu - token, njegov rok i nalog koji je iza njega.
class PrijavaOdgovor {
  const PrijavaOdgovor({
    required this.token,
    required this.isticeUtc,
    required this.korisnik,
  });

  final String token;
  final DateTime isticeUtc;
  final Korisnik korisnik;

  factory PrijavaOdgovor.izJsona(Map<String, dynamic> json) {
    return PrijavaOdgovor(
      token: json['token']?.toString() ?? '',
      isticeUtc: citajDatum(json['isticeUtc']),
      korisnik: Korisnik.izJsona(json['korisnik'] as Map<String, dynamic>),
    );
  }
}
