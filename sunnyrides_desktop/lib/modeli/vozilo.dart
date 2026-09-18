import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Jedno vozilo iz flote, u obliku u kojem ga vracaju i lista i detalji.
///
/// Galerija fotografija nije ovdje - dohvata se zasebno kad zatreba, jer list
/// endpoint ne smije vuci velike podatke. U listi je dovoljan thumbnail.
class Vozilo {
  const Vozilo({
    required this.id,
    required this.modelVozilaId,
    required this.poslovnicaId,
    required this.registarskaOznaka,
    required this.godinaProizvodnje,
    required this.kilometraza,
    required this.aktivno,
    required this.satnaTarifa,
    required this.dnevnaTarifa,
    required this.iznosDepozita,
    required this.kubikaza,
    required this.snagaKw,
    required this.kategorijaDozvoleId,
    this.modelNaziv,
    this.markaNaziv,
    this.tipVozilaNaziv,
    this.tipGorivaNaziv,
    this.kategorijaDozvoleOznaka,
    this.poslovnicaNaziv,
    this.gradNaziv,
    this.thumbnailUrl,
  });

  final int id;
  final int modelVozilaId;
  final int poslovnicaId;
  final String registarskaOznaka;
  final int godinaProizvodnje;
  final int kilometraza;
  final bool aktivno;
  final double satnaTarifa;
  final double dnevnaTarifa;
  final double iznosDepozita;
  final int kubikaza;
  final double snagaKw;
  final int kategorijaDozvoleId;

  final String? modelNaziv;
  final String? markaNaziv;
  final String? tipVozilaNaziv;
  final String? tipGorivaNaziv;
  final String? kategorijaDozvoleOznaka;
  final String? poslovnicaNaziv;
  final String? gradNaziv;
  final String? thumbnailUrl;

  /// Kubikaza, a za elektricna vozila snaga - njima je kubikaza nula, pa bi "0 ccm"
  /// izgledalo kao da podatak nedostaje.
  String get pogon =>
      kubikaza > 0 ? '$kubikaza ccm' : '${snagaKw.toStringAsFixed(1)} kW';

  /// Marka i model zajedno, kako se vozilo svuda i imenuje.
  String get puniNaziv => [
    markaNaziv,
    modelNaziv,
  ].where((x) => x != null && x.isNotEmpty).join(' ');

  factory Vozilo.izJsona(Map<String, dynamic> json) {
    return Vozilo(
      id: citajInt(json['id']),
      modelVozilaId: citajInt(json['modelVozilaId']),
      poslovnicaId: citajInt(json['poslovnicaId']),
      registarskaOznaka: json['registarskaOznaka']?.toString() ?? '',
      godinaProizvodnje: citajInt(json['godinaProizvodnje']),
      kilometraza: citajInt(json['kilometraza']),
      aktivno: citajBool(json['aktivno'], podrazumijevano: true),
      satnaTarifa: citajDouble(json['satnaTarifa']),
      dnevnaTarifa: citajDouble(json['dnevnaTarifa']),
      iznosDepozita: citajDouble(json['iznosDepozita']),
      kubikaza: citajInt(json['kubikaza']),
      snagaKw: citajDouble(json['snagaKw']),
      kategorijaDozvoleId: citajInt(json['kategorijaDozvoleId']),
      modelNaziv: json['modelNaziv']?.toString(),
      markaNaziv: json['markaNaziv']?.toString(),
      tipVozilaNaziv: json['tipVozilaNaziv']?.toString(),
      tipGorivaNaziv: json['tipGorivaNaziv']?.toString(),
      kategorijaDozvoleOznaka: json['kategorijaDozvoleOznaka']?.toString(),
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString(),
      gradNaziv: json['gradNaziv']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
    );
  }
}

/// Jedna fotografija iz galerije vozila.
class SlikaVozila {
  const SlikaVozila({
    required this.id,
    required this.url,
    required this.thumbnailUrl,
    required this.jeGlavna,
    required this.redoslijed,
  });

  final int id;
  final String url;
  final String thumbnailUrl;
  final bool jeGlavna;
  final int redoslijed;

  factory SlikaVozila.izJsona(Map<String, dynamic> json) {
    return SlikaVozila(
      id: citajInt(json['id']),
      url: json['url']?.toString() ?? '',
      thumbnailUrl: json['thumbnailUrl']?.toString() ?? '',
      jeGlavna: citajBool(json['jeGlavna']),
      redoslijed: citajInt(json['redoslijed']),
    );
  }
}

/// Filteri liste vozila. Prazna polja se ne salju.
class UpitVozila {
  const UpitVozila({
    this.stranica = 0,
    this.velicinaStranice = 12,
    this.pretraga,
    this.markaId,
    this.tipVozilaId,
    this.poslovnicaId,
    this.aktivno,
  });

  final int stranica;
  final int velicinaStranice;

  /// Pretrazuje se naziv modela; registarska oznaka ide zasebnim poljem na serveru.
  final String? pretraga;

  final int? markaId;
  final int? tipVozilaId;
  final int? poslovnicaId;
  final bool? aktivno;

  UpitVozila kopija({
    int? stranica,
    String? pretraga,
    int? markaId,
    int? tipVozilaId,
    int? poslovnicaId,
    bool? aktivno,
    bool ocistiMarku = false,
    bool ocistiTip = false,
    bool ocistiPoslovnicu = false,
    bool ocistiAktivno = false,
  }) {
    return UpitVozila(
      stranica: stranica ?? this.stranica,
      velicinaStranice: velicinaStranice,
      pretraga: pretraga ?? this.pretraga,
      markaId: ocistiMarku ? null : (markaId ?? this.markaId),
      tipVozilaId: ocistiTip ? null : (tipVozilaId ?? this.tipVozilaId),
      poslovnicaId: ocistiPoslovnicu
          ? null
          : (poslovnicaId ?? this.poslovnicaId),
      aktivno: ocistiAktivno ? null : (aktivno ?? this.aktivno),
    );
  }

  Map<String, dynamic> uMapu() {
    return {
      'page': stranica,
      'pageSize': velicinaStranice,
      'includeTotalCount': true,
      if (pretraga != null && pretraga!.isNotEmpty) 'modelNaziv': pretraga,
      if (markaId != null) 'markaId': markaId,
      if (tipVozilaId != null) 'tipVozilaId': tipVozilaId,
      if (poslovnicaId != null) 'poslovnicaId': poslovnicaId,
      if (aktivno != null) 'aktivno': aktivno,
    };
  }
}
