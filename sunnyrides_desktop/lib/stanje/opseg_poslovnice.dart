import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

import '../modeli/stavka_sifrarnika.dart';
import 'sesija.dart';

/// Poslovnice koje prijavljeni korisnik smije birati.
///
/// Uposlenik je vezan za svoju poslovnicu, pa mu se u filterima i formama nudi samo
/// ona. Server ga ionako ne pusta dalje od nje - ovdje je da ne bira nesto sto mu
/// ne radi. Administratoru se nude sve.
List<StavkaSifrarnika> dozvoljenePoslovnice(
  BuildContext context,
  List<StavkaSifrarnika> sve,
) {
  final korisnik = context.read<Sesija>().korisnik;

  if (korisnik == null || !korisnik.vezanZaPoslovnicu) {
    return sve;
  }

  return sve.where((x) => x.id == korisnik.poslovnicaId).toList();
}
