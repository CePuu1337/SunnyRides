import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import 'bocna_traka.dart';
import 'meni.dart';
import 'zaglavlje.dart';

/// Okvir aplikacije: bocna traka, zaglavlje i sadrzaj izmedju njih.
class Ljuska extends StatefulWidget {
  const Ljuska({super.key});

  @override
  State<Ljuska> createState() => _LjuskaStanje();
}

class _LjuskaStanje extends State<Ljuska> {
  late List<GrupaMenija> _grupe;
  late StavkaMenija _aktivna;
  late NotifikacijeStanje _notifikacije;

  @override
  void initState() {
    super.initState();

    final korisnik = context.read<Sesija>().korisnik!;

    _grupe = Meni.zaKorisnika(korisnik);
    _aktivna = _grupe.first.stavke.first;

    _notifikacije = NotifikacijeStanje(
      klijent: context.read<ApiKlijent>(),
      okruzenje: context.read<Okruzenje>(),
      pohrana: context.read<PohranaTokena>(),
    );
    _notifikacije.pokreni();
  }

  @override
  void dispose() {
    _notifikacije.dispose();
    super.dispose();
  }

  void _odaberi(StavkaMenija stavka) {
    if (stavka.id == _aktivna.id) {
      return;
    }

    setState(() => _aktivna = stavka);
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<NotifikacijeStanje>.value(
      value: _notifikacije,
      child: Scaffold(
        body: Row(
          children: [
            BocnaTraka(grupe: _grupe, aktivna: _aktivna, naOdabir: _odaberi),
            Expanded(
              child: Column(
                children: [
                  Zaglavlje(
                    stavka: _aktivna,
                    naNotifikacije: () => _notifikacije.osvjezi(),
                  ),
                  Expanded(
                    child: Container(
                      color: Boje.platno,

                      // Svaki ekran dobija svoj Navigator, da detalji koje otvori
                      // ostanu unutar sadrzaja - bocna traka i zaglavlje se ne
                      // prekrivaju. Kljuc nosi identifikator stavke, pa promjena
                      // ekrana pocinje od cistog steka umjesto da pamti tudje detalje.
                      child: Navigator(
                        key: ValueKey(_aktivna.id),
                        onGenerateRoute: (postavke) => MaterialPageRoute<void>(
                          settings: postavke,
                          builder: _aktivna.gradi,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
