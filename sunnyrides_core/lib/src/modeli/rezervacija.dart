import 'enumi.dart';
import 'pretvaranje.dart';

/// Jedna rezervacija. Isti oblik vraca i lista i detalji - razlika je u tome sto
/// detalji popune opremu i historiju statusa, a lista ih ostavlja praznim.
class Rezervacija {
  const Rezervacija({
    required this.id,
    required this.broj,
    required this.datumOd,
    required this.datumDo,
    required this.status,
    required this.ukupanIznos,
    required this.iznosDepozita,
    required this.iznosPopusta,
    required this.isPaid,
    required this.datumKreiranja,
    required this.korisnikId,
    required this.voziloId,
    required this.poslovnicaId,
    required this.stavkeOpreme,
    required this.historijaStatusa,
    this.drziDo,
    this.preostaloSekundiDrzanja,
    this.razlogOtkazivanjaNaziv,
    this.napomenaOtkazivanja,
    this.datumOtkazivanja,
    this.otkazaoKorisnikIme,
    this.klijentImePrezime,
    this.klijentEmail,
    this.registarskaOznaka,
    this.modelNaziv,
    this.markaNaziv,
    this.thumbnailUrl,
    this.poslovnicaNaziv,
    this.paketOsiguranjaNaziv,
  });

  final int id;
  final String broj;
  final DateTime datumOd;
  final DateTime datumDo;
  final StatusRezervacije? status;
  final double ukupanIznos;
  final double iznosDepozita;
  final double iznosPopusta;
  final bool isPaid;
  final DateTime datumKreiranja;

  /// Do kada vozilo ostaje rezervisano bez placanja.
  final DateTime? drziDo;
  final int? preostaloSekundiDrzanja;

  final String? razlogOtkazivanjaNaziv;
  final String? napomenaOtkazivanja;
  final DateTime? datumOtkazivanja;
  final String? otkazaoKorisnikIme;

  final int korisnikId;
  final String? klijentImePrezime;
  final String? klijentEmail;

  final int voziloId;
  final String? registarskaOznaka;
  final String? modelNaziv;
  final String? markaNaziv;
  final String? thumbnailUrl;

  final int poslovnicaId;
  final String? poslovnicaNaziv;
  final String? paketOsiguranjaNaziv;

  final List<StavkaOpreme> stavkeOpreme;
  final List<HistorijaStatusa> historijaStatusa;

  String get vozilo => [
    markaNaziv,
    modelNaziv,
  ].where((x) => x != null && x.isNotEmpty).join(' ');

  bool get seMozeOtkazati =>
      status == StatusRezervacije.naCekanju ||
      status == StatusRezervacije.potvrdjena;

  factory Rezervacija.izJsona(Map<String, dynamic> json) {
    return Rezervacija(
      id: citajInt(json['id']),
      broj: json['broj']?.toString() ?? '',
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      status: StatusRezervacije.izBroja(citajInt(json['status'])),
      ukupanIznos: citajDouble(json['ukupanIznos']),
      iznosDepozita: citajDouble(json['iznosDepozita']),
      iznosPopusta: citajDouble(json['iznosPopusta']),
      isPaid: citajBool(json['isPaid']),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      drziDo: citajDatumIliNista(json['drziDo']),
      preostaloSekundiDrzanja: json['preostaloSekundiDrzanja'] == null
          ? null
          : citajInt(json['preostaloSekundiDrzanja']),
      razlogOtkazivanjaNaziv: json['razlogOtkazivanjaNaziv']?.toString(),
      napomenaOtkazivanja: json['napomenaOtkazivanja']?.toString(),
      datumOtkazivanja: citajDatumIliNista(json['datumOtkazivanja']),
      otkazaoKorisnikIme: json['otkazaoKorisnikIme']?.toString(),
      korisnikId: citajInt(json['korisnikId']),
      klijentImePrezime: json['klijentImePrezime']?.toString(),
      klijentEmail: json['klijentEmail']?.toString(),
      voziloId: citajInt(json['voziloId']),
      registarskaOznaka: json['registarskaOznaka']?.toString(),
      modelNaziv: json['modelNaziv']?.toString(),
      markaNaziv: json['markaNaziv']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      poslovnicaId: citajInt(json['poslovnicaId']),
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString(),
      paketOsiguranjaNaziv: json['paketOsiguranjaNaziv']?.toString(),
      stavkeOpreme: citajListu(json['stavkeOpreme'], StavkaOpreme.izJsona),
      historijaStatusa: citajListu(
        json['historijaStatusa'],
        HistorijaStatusa.izJsona,
      ),
    );
  }
}

class StavkaOpreme {
  const StavkaOpreme({
    required this.naziv,
    required this.kolicina,
    required this.cijenaPoJedinici,
    required this.iznos,
  });

  final String naziv;
  final int kolicina;
  final double cijenaPoJedinici;
  final double iznos;

  factory StavkaOpreme.izJsona(Map<String, dynamic> json) {
    return StavkaOpreme(
      naziv: json['naziv']?.toString() ?? '',
      kolicina: citajInt(json['kolicina']),
      cijenaPoJedinici: citajDouble(json['cijenaPoJedinici']),
      iznos: citajDouble(json['iznos']),
    );
  }
}

/// Jedan korak u zivotu rezervacije. Historija je jedini trag o tome ko je sta
/// promijenio, pa se prikazuje kako je i zapisana.
class HistorijaStatusa {
  const HistorijaStatusa({
    required this.statusU,
    required this.opis,
    required this.datumVrijeme,
    this.statusIz,
    this.razlog,
    this.izvrsioKorisnikIme,
  });

  final StatusRezervacije? statusIz;
  final StatusRezervacije? statusU;
  final String opis;
  final String? razlog;
  final DateTime datumVrijeme;
  final String? izvrsioKorisnikIme;

  factory HistorijaStatusa.izJsona(Map<String, dynamic> json) {
    return HistorijaStatusa(
      statusIz: json['statusIz'] == null
          ? null
          : StatusRezervacije.izBroja(citajInt(json['statusIz'])),
      statusU: StatusRezervacije.izBroja(citajInt(json['statusU'])),
      opis: json['opis']?.toString() ?? '',
      razlog: json['razlog']?.toString(),
      datumVrijeme: citajDatum(json['datumVrijeme']),
      izvrsioKorisnikIme: json['izvrsioKorisnikIme']?.toString(),
    );
  }
}

/// Obracun koji server vrati prije otkazivanja - koliko se vraca i zasto toliko.
///
/// Sve brojke racuna server iz stvarno naplacenog iznosa. Aplikacija ih samo
/// prikazuje; da ih sama izvodi iz cjenovnika, klijentu bi mogla pokazati jedan
/// iznos a na racun stici drugi.
class ObracunOtkazivanja {
  const ObracunOtkazivanja({
    required this.rezervacijaId,
    required this.broj,
    required this.datumOd,
    required this.mozeSeOtkazati,
    required this.danaDoPreuzimanja,
    required this.otkazujeAgencija,
    required this.naplaceno,
    required this.vecVraceno,
    required this.dioDepozita,
    required this.dioNajma,
    required this.procenatPovrataNajma,
    required this.povratNajma,
    required this.povratDepozita,
    required this.ukupanPovrat,
    required this.zadrzanoAgenciji,
    required this.obrazlozenje,
    this.razlogNemogucnosti,
  });

  final int rezervacijaId;
  final String broj;
  final DateTime datumOd;

  /// Kad je ovo netacno, otkazivanje se ne nudi - razlog stoji u [razlogNemogucnosti].
  final bool mozeSeOtkazati;
  final String? razlogNemogucnosti;

  final double danaDoPreuzimanja;

  /// Otkazivanje od strane agencije vraca puni iznos, bez obzira na rok.
  final bool otkazujeAgencija;

  final double naplaceno;
  final double vecVraceno;
  final double dioDepozita;
  final double dioNajma;
  final double procenatPovrataNajma;
  final double povratNajma;
  final double povratDepozita;
  final double ukupanPovrat;
  final double zadrzanoAgenciji;
  final String obrazlozenje;

  factory ObracunOtkazivanja.izJsona(Map<String, dynamic> json) {
    return ObracunOtkazivanja(
      rezervacijaId: citajInt(json['rezervacijaId']),
      broj: json['broj']?.toString() ?? '',
      datumOd: citajDatum(json['datumOd']),
      mozeSeOtkazati: citajBool(json['mozeSeOtkazati']),
      razlogNemogucnosti: json['razlogNemogucnosti']?.toString(),
      danaDoPreuzimanja: citajDouble(json['danaDoPreuzimanja']),
      otkazujeAgencija: citajBool(json['otkazujeAgencija']),
      naplaceno: citajDouble(json['naplaceno']),
      vecVraceno: citajDouble(json['vecVraceno']),
      dioDepozita: citajDouble(json['dioDepozita']),
      dioNajma: citajDouble(json['dioNajma']),
      procenatPovrataNajma: citajDouble(json['procenatPovrataNajma']),
      povratNajma: citajDouble(json['povratNajma']),
      povratDepozita: citajDouble(json['povratDepozita']),
      ukupanPovrat: citajDouble(json['ukupanPovrat']),
      zadrzanoAgenciji: citajDouble(json['zadrzanoAgenciji']),
      obrazlozenje: json['obrazlozenje']?.toString() ?? '',
    );
  }
}
