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
    this.cijenaZaPeriod,
    this.tarifa,
    this.modelNaziv,
    this.markaNaziv,
    this.tipVozilaNaziv,
    this.tipGorivaNaziv,
    this.kategorijaDozvoleOznaka,
    this.poslovnicaNaziv,
    this.gradNaziv,
    this.udaljenostKm,
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

  /// Cijena za termin iz pretrage, izracunata na serveru. Prazna kad pretraga
  /// nema termin.
  final CijenaPerioda? cijenaZaPeriod;

  /// Cijena koju klijent placa na dan preuzimanja, sa sezonom, izracunata na
  /// serveru. [dnevnaTarifa] je osnovna tarifa upisana na vozilu i u sezoni se od
  /// ove razlikuje.
  final TarifaNaDan? tarifa;

  /// Cijena po danu za prikaz. Ista ona po kojoj server filtrira i sortira pretragu.
  double get cijenaPoDanu => tarifa?.dnevnaCijena ?? dnevnaTarifa;

  final String? modelNaziv;
  final String? markaNaziv;
  final String? tipVozilaNaziv;
  final String? tipGorivaNaziv;
  final String? kategorijaDozvoleOznaka;
  final String? poslovnicaNaziv;
  final String? gradNaziv;

  /// Zracna udaljenost poslovnice od klijenta. Prazna kad se ne zna odakle mjeriti.
  final double? udaljenostKm;

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
      cijenaZaPeriod: json['cijenaZaPeriod'] is Map<String, dynamic>
          ? CijenaPerioda.izJsona(
              json['cijenaZaPeriod'] as Map<String, dynamic>,
            )
          : null,
      tarifa: json['tarifa'] is Map<String, dynamic>
          ? TarifaNaDan.izJsona(json['tarifa'] as Map<String, dynamic>)
          : null,
      modelNaziv: json['modelNaziv']?.toString(),
      markaNaziv: json['markaNaziv']?.toString(),
      tipVozilaNaziv: json['tipVozilaNaziv']?.toString(),
      tipGorivaNaziv: json['tipGorivaNaziv']?.toString(),
      kategorijaDozvoleOznaka: json['kategorijaDozvoleOznaka']?.toString(),
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString(),
      gradNaziv: json['gradNaziv']?.toString(),
      udaljenostKm: citajDoubleIliNista(json['udaljenostKm']),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
    );
  }
}

/// Poslovnica najbliza klijentu, za karticu na pocetnom ekranu.
class NajblizaPoslovnica {
  const NajblizaPoslovnica({
    required this.id,
    required this.naziv,
    required this.adresa,
    required this.udaljenostKm,
    required this.poLokaciji,
    this.gradNaziv,
    this.radnoVrijeme,
    this.polazniGrad,
  });

  final int id;
  final String naziv;
  final String adresa;
  final String? gradNaziv;
  final String? radnoVrijeme;
  final double udaljenostKm;

  /// Udaljenost je mjerena od lokacije telefona, a ne od grada iz profila.
  final bool poLokaciji;

  /// Grad iz profila, kad je udaljenost mjerena od njega.
  final String? polazniGrad;

  factory NajblizaPoslovnica.izJsona(Map<String, dynamic> json) {
    return NajblizaPoslovnica(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      adresa: json['adresa']?.toString() ?? '',
      gradNaziv: json['gradNaziv']?.toString(),
      radnoVrijeme: json['radnoVrijeme']?.toString(),
      udaljenostKm: citajDouble(json['udaljenostKm']),
      poLokaciji: citajBool(json['poLokaciji']),
      polazniGrad: json['polazniGrad']?.toString(),
    );
  }
}

/// Najam za cijeli trazeni period, onako kako ga je izracunao server - sa sezonom
/// i popustom za duze najmove, bez opreme, osiguranja i depozita.
class CijenaPerioda {
  const CijenaPerioda({
    required this.iznosNajma,
    required this.naplataPoSatu,
    required this.brojSati,
    required this.brojDana,
    required this.procenatPopusta,
    required this.iznosDepozita,
  });

  final double iznosNajma;
  final bool naplataPoSatu;
  final int brojSati;
  final int brojDana;
  final double procenatPopusta;
  final double iznosDepozita;

  /// Za koliko se naplacuje: "za 3 dana", "za 5 sati". Broj dana je onaj koji se
  /// stvarno naplacuje, sa tolerancijom od 59 minuta, a ne razlika datuma.
  String get opis => naplataPoSatu
      ? 'za $brojSati ${Formati.oblik(brojSati, 'sat', 'sata', 'sati')}'
      : 'za $brojDana ${Formati.oblik(brojDana, 'dan', 'dana', 'dana')}';

  factory CijenaPerioda.izJsona(Map<String, dynamic> json) {
    return CijenaPerioda(
      iznosNajma: citajDouble(json['iznosNajma']),
      naplataPoSatu: citajBool(json['naplataPoSatu']),
      brojSati: citajInt(json['brojSati']),
      brojDana: citajInt(json['brojDana']),
      procenatPopusta: citajDouble(json['procenatPopusta']),
      iznosDepozita: citajDouble(json['iznosDepozita']),
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
  const Stavka({required this.id, required this.naziv, this.gradId});

  final int id;
  final String naziv;

  /// Samo kod poslovnice: grad u kojem je. Filter po poslovnici nudi samo
  /// poslovnice odabranog grada.
  final int? gradId;

  factory Stavka.izJsona(Map<String, dynamic> json) {
    return Stavka(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      gradId: json['gradId'] == null ? null : citajInt(json['gradId']),
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

/// Satna i dnevna cijena i popusti koji vaze za vozilo na odredjeni dan, onako kako
/// ih racuna server - isto pravilo po kojem racuna i rezervaciju.
class TarifaNaDan {
  const TarifaNaDan({
    required this.satnaCijena,
    required this.dnevnaCijena,
    required this.popustPrag1,
    required this.popustProcenat1,
    required this.popustPrag2,
    required this.popustProcenat2,
    this.nazivSezone,
  });

  final double satnaCijena;
  final double dnevnaCijena;
  final String? nazivSezone;

  final int popustPrag1;
  final double popustProcenat1;
  final int popustPrag2;
  final double popustProcenat2;

  factory TarifaNaDan.izJsona(Map<String, dynamic> json) {
    return TarifaNaDan(
      satnaCijena: citajDouble(json['satnaCijena']),
      dnevnaCijena: citajDouble(json['dnevnaCijena']),
      nazivSezone: json['nazivSezone']?.toString(),
      popustPrag1: citajInt(json['popustPrag1']),
      popustProcenat1: citajDouble(json['popustProcenat1']),
      popustPrag2: citajInt(json['popustPrag2']),
      popustProcenat2: citajDouble(json['popustProcenat2']),
    );
  }
}
