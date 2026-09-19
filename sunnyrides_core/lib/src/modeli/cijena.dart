import 'pretvaranje.dart';

/// Obracun cijene koji vrati server prije nego se rezervacija napravi.
///
/// Aplikacija nikad ne racuna iznos sama i nikad ga ne salje. Ovaj obracun postoji
/// samo da se korisniku pokaze koliko ce platiti - server ga pri kreiranju racuna
/// ponovo, iz istih pravila, i tek taj iznos vazi.
class CijenaRezervacije {
  const CijenaRezervacije({
    required this.datumOd,
    required this.datumDo,
    required this.naplataPoSatu,
    required this.brojSati,
    required this.brojDana,
    required this.satnaTarifa,
    required this.dnevnaTarifa,
    required this.mnozilac,
    required this.osnovicaNajma,
    required this.iznosNajma,
    required this.procenatPopusta,
    required this.iznosPopusta,
    required this.oprema,
    required this.iznosOpreme,
    required this.iznosOsiguranja,
    required this.iznosDepozita,
    required this.ukupanIznos,
    this.nazivSezone,
    this.paketOsiguranjaNaziv,
  });

  final DateTime datumOd;
  final DateTime datumDo;
  final bool naplataPoSatu;
  final int brojSati;
  final int brojDana;
  final double satnaTarifa;
  final double dnevnaTarifa;

  /// Sezonski mnozilac cijene. 1 znaci da sezona ne vazi.
  final double mnozilac;
  final String? nazivSezone;

  final double osnovicaNajma;
  final double iznosNajma;
  final double procenatPopusta;
  final double iznosPopusta;
  final List<StavkaCijene> oprema;
  final double iznosOpreme;
  final String? paketOsiguranjaNaziv;
  final double iznosOsiguranja;
  final double iznosDepozita;
  final double ukupanIznos;

  /// Opis trajanja onako kako je naplaceno - po satu ili po danu.
  String get trajanje => naplataPoSatu ? '$brojSati h' : '$brojDana d';

  factory CijenaRezervacije.izJsona(Map<String, dynamic> json) {
    return CijenaRezervacije(
      datumOd: citajDatum(json['datumOd']),
      datumDo: citajDatum(json['datumDo']),
      naplataPoSatu: citajBool(json['naplataPoSatu']),
      brojSati: citajInt(json['brojSati']),
      brojDana: citajInt(json['brojDana']),
      satnaTarifa: citajDouble(json['satnaTarifa']),
      dnevnaTarifa: citajDouble(json['dnevnaTarifa']),
      mnozilac: citajDouble(json['mnozilac'], podrazumijevano: 1),
      nazivSezone: json['nazivSezone']?.toString(),
      osnovicaNajma: citajDouble(json['osnovicaNajma']),
      iznosNajma: citajDouble(json['iznosNajma']),
      procenatPopusta: citajDouble(json['procenatPopusta']),
      iznosPopusta: citajDouble(json['iznosPopusta']),
      oprema: citajListu(json['oprema'], StavkaCijene.izJsona),
      iznosOpreme: citajDouble(json['iznosOpreme']),
      paketOsiguranjaNaziv: json['paketOsiguranjaNaziv']?.toString(),
      iznosOsiguranja: citajDouble(json['iznosOsiguranja']),
      iznosDepozita: citajDouble(json['iznosDepozita']),
      ukupanIznos: citajDouble(json['ukupanIznos']),
    );
  }
}

class StavkaCijene {
  const StavkaCijene({
    required this.vrstaOpremeId,
    required this.naziv,
    required this.kolicina,
    required this.cijenaPoJedinici,
    required this.iznos,
  });

  final int vrstaOpremeId;
  final String naziv;
  final int kolicina;
  final double cijenaPoJedinici;
  final double iznos;

  factory StavkaCijene.izJsona(Map<String, dynamic> json) {
    return StavkaCijene(
      vrstaOpremeId: citajInt(json['vrstaOpremeId']),
      naziv: json['naziv']?.toString() ?? '',
      kolicina: citajInt(json['kolicina']),
      cijenaPoJedinici: citajDouble(json['cijenaPoJedinici']),
      iznos: citajDouble(json['iznos']),
    );
  }
}

/// Dodatna oprema koja se moze iznajmiti uz vozilo.
class VrstaOpreme {
  const VrstaOpreme({
    required this.id,
    required this.naziv,
    this.cijenaPoDanu,
    this.fiksnaCijena,
  });

  final int id;
  final String naziv;
  final double? cijenaPoDanu;
  final double? fiksnaCijena;

  factory VrstaOpreme.izJsona(Map<String, dynamic> json) {
    return VrstaOpreme(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      cijenaPoDanu: citajDoubleIliNista(json['cijenaPoDanu']),
      fiksnaCijena: citajDoubleIliNista(json['fiksnaCijena']),
    );
  }
}

class PaketOsiguranja {
  const PaketOsiguranja({
    required this.id,
    required this.naziv,
    required this.cijenaPoDanu,
    required this.iznosUcesca,
  });

  final int id;
  final String naziv;
  final double cijenaPoDanu;

  /// Koliko klijent placa sam u slucaju stete, uz ovaj paket.
  final double iznosUcesca;

  factory PaketOsiguranja.izJsona(Map<String, dynamic> json) {
    return PaketOsiguranja(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      cijenaPoDanu: citajDouble(json['cijenaPoDanu']),
      iznosUcesca: citajDouble(json['iznosUcesca']),
    );
  }
}

/// Zahtjev za obracun ili kreiranje rezervacije. Iznosa nema - racuna ih server.
class ZahtjevRezervacije {
  const ZahtjevRezervacije({
    required this.voziloId,
    required this.datumOd,
    required this.datumDo,
    this.oprema = const {},
    this.paketOsiguranjaId,
  });

  final int voziloId;
  final DateTime datumOd;
  final DateTime datumDo;

  /// Kolicina po vrsti opreme. Stavke sa nulom se ne salju.
  final Map<int, int> oprema;

  final int? paketOsiguranjaId;

  Map<String, dynamic> uJson() {
    return {
      'voziloId': voziloId,
      'datumOd': datumOd.toUtc().toIso8601String(),
      'datumDo': datumDo.toUtc().toIso8601String(),
      'oprema': [
        for (final unos in oprema.entries)
          if (unos.value > 0) {'vrstaOpremeId': unos.key, 'kolicina': unos.value},
      ],
      'paketOsiguranjaId': paketOsiguranjaId,
    };
  }
}
