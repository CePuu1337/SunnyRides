import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Sve sto pocetni ekran prikazuje, u jednom odgovoru.
class PregledPoslovanja {
  const PregledPoslovanja({
    required this.naDanUtc,
    required this.metrike,
    required this.rasporedDanas,
    required this.poTipuVozila,
    required this.poPoslovnici,
  });

  final DateTime naDanUtc;
  final MetrikePoslovanja metrike;
  final List<StavkaRasporeda> rasporedDanas;
  final List<PresjekFlote> poTipuVozila;
  final List<PresjekFlote> poPoslovnici;

  factory PregledPoslovanja.izJsona(Map<String, dynamic> json) {
    return PregledPoslovanja(
      naDanUtc: citajDatum(json['naDanUtc']),
      metrike: MetrikePoslovanja.izJsona(
        json['metrike'] as Map<String, dynamic>? ?? const {},
      ),
      rasporedDanas: citajListu(json['rasporedDanas'], StavkaRasporeda.izJsona),
      poTipuVozila: citajListu(json['poTipuVozila'], PresjekFlote.izJsona),
      poPoslovnici: citajListu(json['poPoslovnici'], PresjekFlote.izJsona),
    );
  }
}

class MetrikePoslovanja {
  const MetrikePoslovanja({
    required this.ukupnoAktivnihVozila,
    required this.vozilaUNajmu,
    required this.trenutnaIskoristenost,
    required this.aktivneRezervacije,
    required this.naplacenoTekucegMjeseca,
    required this.refundiranoTekucegMjeseca,
    required this.netoPrihodTekucegMjeseca,
    required this.cekaObradu,
    required this.neverifikovaneDozvole,
    required this.neplaceneRezervacije,
    required this.poredba,
  });

  final int ukupnoAktivnihVozila;
  final int vozilaUNajmu;
  final double trenutnaIskoristenost;
  final int aktivneRezervacije;
  final double naplacenoTekucegMjeseca;
  final double refundiranoTekucegMjeseca;
  final double netoPrihodTekucegMjeseca;
  final int cekaObradu;
  final int neverifikovaneDozvole;
  final int neplaceneRezervacije;
  final PoredbaPerioda poredba;

  factory MetrikePoslovanja.izJsona(Map<String, dynamic> json) {
    return MetrikePoslovanja(
      ukupnoAktivnihVozila: citajInt(json['ukupnoAktivnihVozila']),
      vozilaUNajmu: citajInt(json['vozilaUNajmu']),
      trenutnaIskoristenost: citajDouble(json['trenutnaIskoristenost']),
      aktivneRezervacije: citajInt(json['aktivneRezervacije']),
      naplacenoTekucegMjeseca: citajDouble(json['naplacenoTekucegMjeseca']),
      refundiranoTekucegMjeseca: citajDouble(json['refundiranoTekucegMjeseca']),
      netoPrihodTekucegMjeseca: citajDouble(json['netoPrihodTekucegMjeseca']),
      cekaObradu: citajInt(json['cekaObradu']),
      neverifikovaneDozvole: citajInt(json['neverifikovaneDozvole']),
      neplaceneRezervacije: citajInt(json['neplaceneRezervacije']),
      poredba: PoredbaPerioda.izJsona(
        json['poredba'] as Map<String, dynamic>? ?? const {},
      ),
    );
  }
}

/// Tekuci mjesec do danas naspram istog broja dana proslog mjeseca.
class PoredbaPerioda {
  const PoredbaPerioda({
    required this.tekuciOd,
    required this.tekuciDo,
    required this.prethodniOd,
    required this.prethodniDo,
    required this.netoPrihod,
    required this.noveRezervacije,
    required this.zavrseneRezervacije,
  });

  final DateTime tekuciOd;
  final DateTime tekuciDo;
  final DateTime prethodniOd;
  final DateTime prethodniDo;

  final PoredbaMetrike netoPrihod;
  final PoredbaMetrike noveRezervacije;
  final PoredbaMetrike zavrseneRezervacije;

  factory PoredbaPerioda.izJsona(Map<String, dynamic> json) {
    PoredbaMetrike metrika(String kljuc) {
      return PoredbaMetrike.izJsona(
        json[kljuc] as Map<String, dynamic>? ?? const {},
      );
    }

    return PoredbaPerioda(
      tekuciOd: citajDatum(json['tekuciOd']),
      tekuciDo: citajDatum(json['tekuciDo']),
      prethodniOd: citajDatum(json['prethodniOd']),
      prethodniDo: citajDatum(json['prethodniDo']),
      netoPrihod: metrika('netoPrihod'),
      noveRezervacije: metrika('noveRezervacije'),
      zavrseneRezervacije: metrika('zavrseneRezervacije'),
    );
  }
}

class PoredbaMetrike {
  const PoredbaMetrike({
    required this.tekuce,
    required this.prethodno,
    required this.promjenaPosto,
  });

  final double tekuce;
  final double prethodno;

  /// Prazno kad proslog mjeseca nije bilo nicega. Tada se strelica ne crta.
  final double? promjenaPosto;

  factory PoredbaMetrike.izJsona(Map<String, dynamic> json) {
    return PoredbaMetrike(
      tekuce: citajDouble(json['tekuce']),
      prethodno: citajDouble(json['prethodno']),
      promjenaPosto: citajDoubleIliNista(json['promjenaPosto']),
    );
  }
}

/// Jedno preuzimanje ili vracanje zakazano za danas.
class StavkaRasporeda {
  const StavkaRasporeda({
    required this.rezervacijaId,
    required this.broj,
    required this.tip,
    required this.vrijeme,
    required this.vozilo,
    required this.registarskaOznaka,
    required this.klijent,
    required this.poslovnica,
    required this.status,
    required this.evidentirano,
  });

  final int rezervacijaId;
  final String broj;
  final TipPrimopredaje? tip;
  final DateTime vrijeme;
  final String vozilo;
  final String registarskaOznaka;
  final String klijent;
  final String poslovnica;
  final StatusRezervacije? status;

  /// Je li primopredaja tog tipa vec evidentirana.
  final bool evidentirano;

  factory StavkaRasporeda.izJsona(Map<String, dynamic> json) {
    return StavkaRasporeda(
      rezervacijaId: citajInt(json['rezervacijaId']),
      broj: json['broj']?.toString() ?? '',
      tip: TipPrimopredaje.izBroja(citajInt(json['tip'])),
      vrijeme: citajDatum(json['vrijeme']),
      vozilo: json['vozilo']?.toString() ?? '',
      registarskaOznaka: json['registarskaOznaka']?.toString() ?? '',
      klijent: json['klijent']?.toString() ?? '',
      poslovnica: json['poslovnica']?.toString() ?? '',
      status: StatusRezervacije.izBroja(citajInt(json['status'])),
      evidentirano: citajBool(json['evidentirano']),
    );
  }
}

/// Presjek flote po tipu vozila ili po poslovnici - isti oblik za oba.
class PresjekFlote {
  const PresjekFlote({
    required this.naziv,
    required this.brojVozila,
    required this.uNajmu,
  });

  final String naziv;
  final int brojVozila;
  final int uNajmu;

  double get udio => brojVozila == 0 ? 0 : uNajmu / brojVozila;

  factory PresjekFlote.izJsona(Map<String, dynamic> json) {
    return PresjekFlote(
      naziv: json['naziv']?.toString() ?? '',
      brojVozila: citajInt(json['brojVozila']),
      uNajmu: citajInt(json['uNajmu']),
    );
  }
}
