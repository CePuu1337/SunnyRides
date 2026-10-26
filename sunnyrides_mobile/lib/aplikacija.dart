import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'ekrani/dozvola/moja_dozvola_ekran.dart';
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
        // Poslije registracije klijent prolazi dva koraka, istim redom i kad
        // aplikaciju zatvori na pola: prvo grad (po njemu se zna koje su mu
        // poslovnice najblize), pa vozacka dozvola.
        StanjeSesije.prijavljen =>
          sesija.korisnik?.trebaOdabratiGrad ?? false
              ? const OdabirGradaEkran()
              : sesija.trebaPrijavitiDozvolu
              ? const MojaDozvolaEkran(korakRegistracije: true)
              : const Ljuska(),
      },
    );
  }
}
