import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Filteri liste rezervacija.
class UpitRezervacija {
  const UpitRezervacija({
    this.stranica = 0,
    this.velicinaStranice = 12,
    this.broj,
    this.klijent,
    this.status,
    this.poslovnicaId,
    this.periodOd,
    this.periodDo,
    this.isPaid,
  });

  final int stranica;
  final int velicinaStranice;
  final String? broj;
  final String? klijent;
  final StatusRezervacije? status;
  final int? poslovnicaId;
  final DateTime? periodOd;
  final DateTime? periodDo;
  final bool? isPaid;

  UpitRezervacija kopija({
    int? stranica,
    String? broj,
    String? klijent,
    StatusRezervacije? status,
    int? poslovnicaId,
    DateTime? periodOd,
    DateTime? periodDo,
    bool? isPaid,
    bool ocistiStatus = false,
    bool ocistiPoslovnicu = false,
    bool ocistiPeriod = false,
    bool ocistiPlaceno = false,
  }) {
    return UpitRezervacija(
      stranica: stranica ?? this.stranica,
      velicinaStranice: velicinaStranice,
      broj: broj ?? this.broj,
      klijent: klijent ?? this.klijent,
      status: ocistiStatus ? null : (status ?? this.status),
      poslovnicaId: ocistiPoslovnicu
          ? null
          : (poslovnicaId ?? this.poslovnicaId),
      periodOd: ocistiPeriod ? null : (periodOd ?? this.periodOd),
      periodDo: ocistiPeriod ? null : (periodDo ?? this.periodDo),
      isPaid: ocistiPlaceno ? null : (isPaid ?? this.isPaid),
    );
  }

  Map<String, dynamic> uMapu() {
    return {
      'page': stranica,
      'pageSize': velicinaStranice,
      'includeTotalCount': true,
      'orderBy': '-datumKreiranja',
      if (broj != null && broj!.isNotEmpty) 'broj': broj,
      if (klijent != null && klijent!.isNotEmpty) 'klijent': klijent,
      if (status != null) 'status': status!.vrijednost,
      if (poslovnicaId != null) 'poslovnicaId': poslovnicaId,
      if (periodOd != null) 'periodOd': periodOd,
      if (periodDo != null) 'periodDo': periodDo,
      if (isPaid != null) 'isPaid': isPaid,
    };
  }
}
