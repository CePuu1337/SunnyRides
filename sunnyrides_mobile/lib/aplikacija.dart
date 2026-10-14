import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'ekrani/prijava/prijava_ekran.dart';
import 'ekrani/ljuska/ljuska.dart';
import 'ekrani/profil/odabir_grada_ekran.dart';
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
    final sesija = context.watch<Sesija>();
    final stanje = sesija.stanje;

    // Promjena sesije zatvara i sve otvoreno preko pocetnog ekrana: registraciju,
    // dijaloge, detalje.
    return ZatvoriEkraneNaPromjenu(
      kljuc: stanje,
      child: switch (stanje) {
        StanjeSesije.provjera => const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        ),
        StanjeSesije.neprijavljen => const PrijavaEkran(),
        // Klijent bez grada ga prvo bira. Tek tada aplikacija zna koje su mu
        // poslovnice najblize.
        StanjeSesije.prijavljen =>
          sesija.korisnik?.trebaOdabratiGrad ?? false
              ? const OdabirGradaEkran()
              : const Ljuska(),
      },
    );
  }
}
