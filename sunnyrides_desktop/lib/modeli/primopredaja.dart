import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Jedno zakazano preuzimanje ili vracanje u rasporedu za dan.
class RasporedStavka {
  const RasporedStavka({
    required this.rezervacijaId,
    required this.broj,
    required this.akcija,
    required this.vrijeme,
    required this.datumOd,
    required this.datumDo,
    required this.statusRezervacije,
    required this.obavljeno,
    required this.izdavanjeEvidentirano,
    required this.jeElektricno,
    this.voziloNaziv,
    this.registarskaOznaka,
    this.klijentImePrezime,
    this.poslovnicaNaziv,
  });

  final int rezervacijaId;
  final String broj;
  final TipPrimopredaje? akcija;
  final DateTime vrijeme;

  /// Cijeli ugovoreni termin, za granice pri naknadnom unosu.
  final DateTime datumOd;
  final DateTime datumDo;
  final String? voziloNaziv;

  /// Vozilo na struju. Forma primopredaje tada trazi napunjenost baterije.
  final bool jeElektricno;
  final String? registarskaOznaka;
  final String? klijentImePrezime;
  final String? poslovnicaNaziv;
  final StatusRezervacije? statusRezervacije;

  /// Je li primopredaja za ovu akciju vec evidentirana.
  final bool obavljeno;

  /// Je li izdavanje vozila evidentirano. Red za vracanje bez toga nema sta zaprimiti.
  final bool izdavanjeEvidentirano;

  /// Sta se po ovom redu moze uraditi.
  bool get trebaIzdavanje =>
      akcija == TipPrimopredaje.povrat && !izdavanjeEvidentirano;

  factory RasporedStavka.izJsona(Map<String, dynamic> json) {
    return RasporedStavka(
      rezervacijaId: citajInt(json['rezervacijaId']),
      broj: json['broj']?.toString() ?? '',
      akcija: TipPrimopredaje.izBroja(citajInt(json['akcija'])),
      vrijeme: citajDatum(json['vrijeme']),
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      voziloNaziv: json['voziloNaziv']?.toString(),
      jeElektricno: citajBool(json['jeElektricno']),
      registarskaOznaka: json['registarskaOznaka']?.toString(),
      klijentImePrezime: json['klijentImePrezime']?.toString(),
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString(),
      statusRezervacije: StatusRezervacije.izBroja(
        citajInt(json['statusRezervacije']),
      ),
      obavljeno: citajBool(json['obavljeno']),
      izdavanjeEvidentirano: citajBool(json['izdavanjeEvidentirano']),
    );
  }
}

/// Zapis o izdavanju ili povratu vozila.
class Primopredaja {
  const Primopredaja({
    required this.id,
    required this.rezervacijaId,
    required this.tip,
    required this.datumVrijeme,
    required this.kilometraza,
    required this.nivoGoriva,
    required this.kontrolnaListaProdjena,
    required this.imaOstecenje,
    required this.fotografijaIds,
    this.rezervacijaBroj,
    this.napomena,
    this.izvrsioKorisnikIme,
    this.opisStete,
    this.iznosStete,
  });

  final int id;
  final int rezervacijaId;
  final String? rezervacijaBroj;
  final TipPrimopredaje? tip;
  final DateTime datumVrijeme;
  final int kilometraza;

  /// Procenat punog rezervoara, od 0 do 100.
  final int nivoGoriva;

  final bool kontrolnaListaProdjena;
  final String? napomena;
  final String? izvrsioKorisnikIme;
  final List<int> fotografijaIds;
  final bool imaOstecenje;
  final String? opisStete;
  final double? iznosStete;

  factory Primopredaja.izJsona(Map<String, dynamic> json) {
    return Primopredaja(
      id: citajInt(json['id']),
      rezervacijaId: citajInt(json['rezervacijaId']),
      rezervacijaBroj: json['rezervacijaBroj']?.toString(),
      tip: TipPrimopredaje.izBroja(citajInt(json['tip'])),
      datumVrijeme: citajDatum(json['datumVrijeme']),
      kilometraza: citajInt(json['kilometraza']),
      nivoGoriva: citajInt(json['nivoGoriva']),
      kontrolnaListaProdjena: citajBool(json['kontrolnaListaProdjena']),
      napomena: json['napomena']?.toString(),
      izvrsioKorisnikIme: json['izvrsioKorisnikIme']?.toString(),
      fotografijaIds: (json['fotografijaIds'] as List<dynamic>? ?? const [])
          .map(citajInt)
          .toList(),
      imaOstecenje: citajBool(json['imaOstecenje']),
      opisStete: json['opisStete']?.toString(),
      iznosStete: citajDoubleIliNista(json['iznosStete']),
    );
  }
}

/// Obracun depozita pri povratu vozila, zajedno sa podacima sa izdavanja.
///
/// Sve brojke racuna server. Uposlenik ih vidi prije nego potvrdi povrat, pa moze
/// klijentu reci koliko se vraca prije nego se bilo sta upise.
class ObracunPovrata {
  const ObracunPovrata({
    required this.rezervacijaId,
    required this.broj,
    required this.ugovorenoVracanje,
    required this.datumPovrata,
    required this.kasnjenjeMinuta,
    required this.unutarTolerancije,
    required this.danaPrekoracenja,
    required this.dnevnaCijena,
    required this.doplata,
    required this.iznosStete,
    required this.uplaceniDepozit,
    required this.zadrzanoOdDepozita,
    required this.povratDepozita,
    required this.nepokrivenoDepozitom,
    required this.obrazlozenje,
    required this.brojFotografijaPriIzdavanju,
    this.kilometrazaPriIzdavanju,
    this.nivoGorivaPriIzdavanju,
    this.datumIzdavanja,
    this.izdaoKorisnikIme,
  });

  final int rezervacijaId;
  final String broj;
  final DateTime ugovorenoVracanje;
  final DateTime datumPovrata;

  /// Koliko je vozilo vraceno kasnije od ugovorenog.
  final int kasnjenjeMinuta;

  /// Kasnjenje unutar dozvoljenog, pa se ne naplacuje.
  final bool unutarTolerancije;

  final int danaPrekoracenja;
  final double dnevnaCijena;
  final double doplata;
  final double iznosStete;
  final double uplaceniDepozit;
  final double zadrzanoOdDepozita;
  final double povratDepozita;

  /// Dio stete i doplate koji depozit ne pokriva.
  final double nepokrivenoDepozitom;

  final String obrazlozenje;

  final int? kilometrazaPriIzdavanju;
  final int? nivoGorivaPriIzdavanju;
  final DateTime? datumIzdavanja;
  final String? izdaoKorisnikIme;
  final int brojFotografijaPriIzdavanju;

  bool get jeIzdato => datumIzdavanja != null;

  factory ObracunPovrata.izJsona(Map<String, dynamic> json) {
    return ObracunPovrata(
      rezervacijaId: citajInt(json['rezervacijaId']),
      broj: json['broj']?.toString() ?? '',
      ugovorenoVracanje: citajDatum(json['ugovorenoVracanje']),
      datumPovrata: citajDatum(json['datumPovrata']),
      kasnjenjeMinuta: citajInt(json['kasnjenjeMinuta']),
      unutarTolerancije: citajBool(json['unutarTolerancije']),
      danaPrekoracenja: citajInt(json['danaPrekoracenja']),
      dnevnaCijena: citajDouble(json['dnevnaCijena']),
      doplata: citajDouble(json['doplata']),
      iznosStete: citajDouble(json['iznosStete']),
      uplaceniDepozit: citajDouble(json['uplaceniDepozit']),
      zadrzanoOdDepozita: citajDouble(json['zadrzanoOdDepozita']),
      povratDepozita: citajDouble(json['povratDepozita']),
      nepokrivenoDepozitom: citajDouble(json['nepokrivenoDepozitom']),
      obrazlozenje: json['obrazlozenje']?.toString() ?? '',
      kilometrazaPriIzdavanju: json['kilometrazaPriIzdavanju'] == null
          ? null
          : citajInt(json['kilometrazaPriIzdavanju']),
      nivoGorivaPriIzdavanju: json['nivoGorivaPriIzdavanju'] == null
          ? null
          : citajInt(json['nivoGorivaPriIzdavanju']),
      datumIzdavanja: citajDatumIliNista(json['datumIzdavanja']),
      izdaoKorisnikIme: json['izdaoKorisnikIme']?.toString(),
      brojFotografijaPriIzdavanju: citajInt(
        json['brojFotografijaPriIzdavanju'],
      ),
    );
  }
}
