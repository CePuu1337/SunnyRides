import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Filteri liste dozvola.
class UpitDozvola {
  const UpitDozvola({
    this.stranica = 0,
    this.velicinaStranice = 20,
    this.klijent,
    this.status = StatusDozvole.naCekanju,
    this.samoIstekle,
  });

  final int stranica;
  final int velicinaStranice;
  final String? klijent;

  /// Podrazumijevano se otvara ono sto ceka obradu - to je posao zbog kojeg se ekran
  /// i otvara. Ostale dozvole se gledaju kad ih neko namjerno potrazi.
  final StatusDozvole? status;

  final bool? samoIstekle;

  UpitDozvola kopija({
    int? stranica,
    String? klijent,
    StatusDozvole? status,
    bool? samoIstekle,
    bool ocistiStatus = false,
    bool ocistiIstekle = false,
  }) {
    return UpitDozvola(
      stranica: stranica ?? this.stranica,
      velicinaStranice: velicinaStranice,
      klijent: klijent ?? this.klijent,
      status: ocistiStatus ? null : (status ?? this.status),
      samoIstekle: ocistiIstekle ? null : (samoIstekle ?? this.samoIstekle),
    );
  }

  Map<String, dynamic> uMapu() {
    return {
      'page': stranica,
      'pageSize': velicinaStranice,
      'includeTotalCount': true,
      if (klijent != null && klijent!.isNotEmpty) 'klijent': klijent,
      if (status != null) 'status': status!.vrijednost,
      if (samoIstekle != null) 'samoIstekle': samoIstekle,
    };
  }
}
