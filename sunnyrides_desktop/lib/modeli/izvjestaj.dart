import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Iskoristenost flote kroz period, po vozilu.
class IskoristenostFlote {
  const IskoristenostFlote({
    required this.od,
    required this.doDatuma,
    required this.generisanoUtc,
    required this.stavke,
    required this.zbir,
    required this.poTipuVozila,
    this.poslovnica,
  });

  final DateTime od;
  final DateTime doDatuma;
  final String? poslovnica;
  final DateTime generisanoUtc;
  final List<StavkaIskoristenosti> stavke;
  final ZbirIskoristenosti zbir;
  final List<IskoristenostPoTipu> poTipuVozila;

  factory IskoristenostFlote.izJsona(Map<String, dynamic> json) {
    return IskoristenostFlote(
      od: citajDatum(json['od']),
      doDatuma: citajDatum(json['do']),
      poslovnica: json['poslovnica']?.toString(),
      generisanoUtc: citajDatum(json['generisanoUtc']),
      stavke: citajListu(json['stavke'], StavkaIskoristenosti.izJsona),
      zbir: ZbirIskoristenosti.izJsona(
        json['zbir'] as Map<String, dynamic>? ?? const {},
      ),
      poTipuVozila: citajListu(
        json['poTipuVozila'],
        IskoristenostPoTipu.izJsona,
      ),
    );
  }
}

class StavkaIskoristenosti {
  const StavkaIskoristenosti({
    required this.vozilo,
    required this.registarskaOznaka,
    required this.tipVozila,
    required this.poslovnica,
    required this.brojNajmova,
    required this.danaIzdato,
    required this.iskoristenost,
    required this.prihod,
    required this.brojOcjena,
    this.prosjecnaOcjena,
  });

  final String vozilo;
  final String registarskaOznaka;
  final String tipVozila;
  final String poslovnica;
  final int brojNajmova;
  final double danaIzdato;
  final double iskoristenost;
  final double prihod;
  final double? prosjecnaOcjena;
  final int brojOcjena;

  factory StavkaIskoristenosti.izJsona(Map<String, dynamic> json) {
    return StavkaIskoristenosti(
      vozilo: json['vozilo']?.toString() ?? '',
      registarskaOznaka: json['registarskaOznaka']?.toString() ?? '',
      tipVozila: json['tipVozila']?.toString() ?? '',
      poslovnica: json['poslovnica']?.toString() ?? '',
      brojNajmova: citajInt(json['brojNajmova']),
      danaIzdato: citajDouble(json['danaIzdato']),
      iskoristenost: citajDouble(json['iskoristenost']),
      prihod: citajDouble(json['prihod']),
      prosjecnaOcjena: citajDoubleIliNista(json['prosjecnaOcjena']),
      brojOcjena: citajInt(json['brojOcjena']),
    );
  }
}

class ZbirIskoristenosti {
  const ZbirIskoristenosti({
    required this.brojVozila,
    required this.brojNajmova,
    required this.danaIzdato,
    required this.iskoristenost,
    required this.prihod,
    this.prosjecnaOcjena,
  });

  final int brojVozila;
  final int brojNajmova;
  final double danaIzdato;
  final double iskoristenost;
  final double prihod;
  final double? prosjecnaOcjena;

  factory ZbirIskoristenosti.izJsona(Map<String, dynamic> json) {
    return ZbirIskoristenosti(
      brojVozila: citajInt(json['brojVozila']),
      brojNajmova: citajInt(json['brojNajmova']),
      danaIzdato: citajDouble(json['danaIzdato']),
      iskoristenost: citajDouble(json['iskoristenost']),
      prihod: citajDouble(json['prihod']),
      prosjecnaOcjena: citajDoubleIliNista(json['prosjecnaOcjena']),
    );
  }
}

class IskoristenostPoTipu {
  const IskoristenostPoTipu({
    required this.tipVozila,
    required this.brojVozila,
    required this.brojNajmova,
    required this.danaIzdato,
    required this.iskoristenost,
    required this.prihod,
  });

  final String tipVozila;
  final int brojVozila;
  final int brojNajmova;
  final double danaIzdato;
  final double iskoristenost;
  final double prihod;

  factory IskoristenostPoTipu.izJsona(Map<String, dynamic> json) {
    return IskoristenostPoTipu(
      tipVozila: json['tipVozila']?.toString() ?? '',
      brojVozila: citajInt(json['brojVozila']),
      brojNajmova: citajInt(json['brojNajmova']),
      danaIzdato: citajDouble(json['danaIzdato']),
      iskoristenost: citajDouble(json['iskoristenost']),
      prihod: citajDouble(json['prihod']),
    );
  }
}

/// Finansijski pregled po mjesecima i poslovnicama.
///
/// Sve brojke dolaze iz placanja, ne iz iznosa rezervacija: rezervacija nosi koliko
/// je trebalo naplatiti, a placanje koliko jeste.
class FinansijskiPregled {
  const FinansijskiPregled({
    required this.od,
    required this.doDatuma,
    required this.generisanoUtc,
    required this.stavke,
    required this.zbir,
    this.poslovnica,
  });

  final DateTime od;
  final DateTime doDatuma;
  final String? poslovnica;
  final DateTime generisanoUtc;
  final List<StavkaFinansijskog> stavke;
  final ZbirFinansijskog zbir;

  factory FinansijskiPregled.izJsona(Map<String, dynamic> json) {
    return FinansijskiPregled(
      od: citajDatum(json['od']),
      doDatuma: citajDatum(json['do']),
      poslovnica: json['poslovnica']?.toString(),
      generisanoUtc: citajDatum(json['generisanoUtc']),
      stavke: citajListu(json['stavke'], StavkaFinansijskog.izJsona),
      zbir: ZbirFinansijskog.izJsona(
        json['zbir'] as Map<String, dynamic>? ?? const {},
      ),
    );
  }
}

class StavkaFinansijskog {
  const StavkaFinansijskog({
    required this.period,
    required this.poslovnica,
    required this.brojRezervacija,
    required this.naplaceno,
    required this.refundirano,
    required this.netoPrihod,
    required this.prosjecnaVrijednostNajma,
  });

  final String period;
  final String poslovnica;
  final int brojRezervacija;
  final double naplaceno;
  final double refundirano;
  final double netoPrihod;
  final double prosjecnaVrijednostNajma;

  factory StavkaFinansijskog.izJsona(Map<String, dynamic> json) {
    return StavkaFinansijskog(
      period: json['period']?.toString() ?? '',
      poslovnica: json['poslovnica']?.toString() ?? '',
      brojRezervacija: citajInt(json['brojRezervacija']),
      naplaceno: citajDouble(json['naplaceno']),
      refundirano: citajDouble(json['refundirano']),
      netoPrihod: citajDouble(json['netoPrihod']),
      prosjecnaVrijednostNajma: citajDouble(json['prosjecnaVrijednostNajma']),
    );
  }
}

class ZbirFinansijskog {
  const ZbirFinansijskog({
    required this.brojRezervacija,
    required this.naplaceno,
    required this.refundirano,
    required this.netoPrihod,
    required this.prosjecnaVrijednostNajma,
  });

  final int brojRezervacija;
  final double naplaceno;
  final double refundirano;
  final double netoPrihod;
  final double prosjecnaVrijednostNajma;

  factory ZbirFinansijskog.izJsona(Map<String, dynamic> json) {
    return ZbirFinansijskog(
      brojRezervacija: citajInt(json['brojRezervacija']),
      naplaceno: citajDouble(json['naplaceno']),
      refundirano: citajDouble(json['refundirano']),
      netoPrihod: citajDouble(json['netoPrihod']),
      prosjecnaVrijednostNajma: citajDouble(json['prosjecnaVrijednostNajma']),
    );
  }
}
