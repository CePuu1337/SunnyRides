import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'ekrani/prijava/prijava_ekran.dart';
import 'ekrani/ljuska/ljuska.dart';
import 'stanje/sesija.dart';

class MobilnaAplikacija extends StatelessWidget {
  const MobilnaAplikacija({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SunnyRides',
      debugShowCheckedModeBanner: false,
      theme: Tema.mobilna(),
      home: const _Korijen(),
    );
  }
}

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
