import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Vozilo iz ponude, onako kako ga klijent vidi.
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
    required this.jeElektricno,
    required this.kategorijaDozvoleId,
    this.prosjecnaOcjena,
    this.brojRecenzija = 0,
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
  final bool jeElektricno;
  final int kategorijaDozvoleId;

  /// Prosjek neskrivenih recenzija. Prazno kad vozilo jos nije ocijenjeno - nula bi
  /// se citala kao losa ocjena.
  final double? prosjecnaOcjena;

  /// Broj recenzija iza prosjeka, da se zna koliko tezine ocjena ima.
  final int brojRecenzija;

  final String? modelNaziv;
  final String? markaNaziv;
  final String? tipVozilaNaziv;
  final String? tipGorivaNaziv;
  final String? kategorijaDozvoleOznaka;
  final String? poslovnicaNaziv;
  final String? gradNaziv;
  final String? thumbnailUrl;

  String get naziv {
    final dijelovi = [
      markaNaziv,
      modelNaziv,
    ].where((x) => x != null && x.isNotEmpty).join(' ');

    return dijelovi.isEmpty ? registarskaOznaka : dijelovi;
  }

  /// Kubikaza, a za vozila na struju snaga - kod njih kubikaza ne postoji.
  String get pogon => jeElektricno || kubikaza == 0
      ? '${snagaKw.toStringAsFixed(1)} kW'
      : '$kubikaza ccm';

  String get lokacija {
    final dijelovi = [
      poslovnicaNaziv,
      gradNaziv,
    ].where((x) => x != null && x.isNotEmpty).toList();

    return dijelovi.join(', ');
  }

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
      jeElektricno: citajBool(json['jeElektricno']),
      kategorijaDozvoleId: citajInt(json['kategorijaDozvoleId']),
      prosjecnaOcjena: citajDoubleIliNista(json['prosjecnaOcjena']),
      brojRecenzija: citajInt(json['brojRecenzija']),
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

/// Preporuceno vozilo, zajedno sa razlogom zbog kojeg je predlozeno.
class Preporuka {
  const Preporuka({
    required this.vozilo,
    required this.skor,
    required this.obrazlozenje,
    this.metoda,
    this.predvidjenaOcjena,
  });

  final Vozilo vozilo;
  final double skor;
  final MetodaPreporuke? metoda;

  /// Ocjena koju model predvidja da bi korisnik dao ovom vozilu.
  final double? predvidjenaOcjena;

  /// Recenica koja objasnjava preporuku. Gradi je server, od signala koji su joj
  /// stvarno najvise doprinijeli - aplikacija je samo prikazuje.
  final String obrazlozenje;

  factory Preporuka.izJsona(Map<String, dynamic> json) {
    final vozilo = json['vozilo'];

    return Preporuka(
      vozilo: Vozilo.izJsona(
        vozilo is Map<String, dynamic> ? vozilo : const {},
      ),
      skor: citajDouble(json['skor']),
      metoda: MetodaPreporuke.izBroja(citajInt(json['metoda'])),
      predvidjenaOcjena: citajDoubleIliNista(json['predvidjenaOcjena']),
      obrazlozenje: json['obrazlozenje']?.toString() ?? '',
    );
  }
}

/// Javna objava agencije.
class Obavijest {
  const Obavijest({
    required this.id,
    required this.naslov,
    required this.tekst,
    required this.datumObjave,
    this.slikaUrl,
    this.thumbnailUrl,
  });

  final int id;
  final String naslov;
  final String tekst;
  final String? slikaUrl;
  final String? thumbnailUrl;
  final DateTime datumObjave;

  factory Obavijest.izJsona(Map<String, dynamic> json) {
    return Obavijest(
      id: citajInt(json['id']),
      naslov: json['naslov']?.toString() ?? '',
      tekst: json['tekst']?.toString() ?? '',
      slikaUrl: json['slikaUrl']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      datumObjave: citajDatum(json['datumObjave']),
    );
  }
}

/// Stavka sifarnika sa nazivom - tip vozila, grad, poslovnica.
class Stavka {
  const Stavka({required this.id, required this.naziv});

  final int id;
  final String naziv;

  factory Stavka.izJsona(Map<String, dynamic> json) {
    return Stavka(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
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
  });

  final int id;
  final String url;
  final String thumbnailUrl;
  final bool jeGlavna;

  factory SlikaVozila.izJsona(Map<String, dynamic> json) {
    return SlikaVozila(
      id: citajInt(json['id']),
      url: json['url']?.toString() ?? '',
      thumbnailUrl: json['thumbnailUrl']?.toString() ?? '',
      jeGlavna: citajBool(json['jeGlavna']),
    );
  }
}
