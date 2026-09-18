import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'ekrani/ljuska/ljuska.dart';
import 'ekrani/prijava/prijava_ekran.dart';
import 'stanje/sesija.dart';

class DesktopAplikacija extends StatelessWidget {
  const DesktopAplikacija({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SunnyRides',
      debugShowCheckedModeBanner: false,
      theme: Tema.desktop(),
      home: const _Korijen(),
    );
  }
}

/// Bira sta se prikazuje prema stanju sesije.
///
/// Ovdje nema Navigatora sa rutama prijava/pocetna: odjava bi tada morala pamtiti
/// koliko ekrana da skine sa steka. Ovako se prikaz mijenja sam kad se promijeni
/// stanje, i nema nacina da se ostane na ekranu nakon odjave.
class _Korijen extends StatelessWidget {
  const _Korijen();

  @override
  Widget build(BuildContext context) {
    final stanje = context.watch<Sesija>().stanje;

    switch (stanje) {
      case StanjeSesije.provjera:
        return const Scaffold(body: Center(child: CircularProgressIndicator()));
      case StanjeSesije.neprijavljen:
        return const PrijavaEkran();
      case StanjeSesije.prijavljen:
        return const Ljuska();
    }
  }
}
