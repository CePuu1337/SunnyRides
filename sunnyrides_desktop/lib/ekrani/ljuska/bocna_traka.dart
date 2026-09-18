import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'meni.dart';

/// Tamna traka sa lijeve strane. Stoji uvijek, mijenja se samo sadrzaj pored nje.
class BocnaTraka extends StatelessWidget {
  const BocnaTraka({
    super.key,
    required this.grupe,
    required this.aktivna,
    required this.naOdabir,
  });

  final List<GrupaMenija> grupe;
  final StavkaMenija aktivna;
  final ValueChanged<StavkaMenija> naOdabir;

  static const sirina = 248.0;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: sirina,
      color: Boje.navy,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _Zaglavlje(),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: Razmaci.m),
              children: [
                for (final grupa in grupe) ...[
                  if (grupa.naslov != null) _NaslovGrupe(grupa.naslov!),
                  for (final stavka in grupa.stavke)
                    _Stavka(
                      stavka: stavka,
                      aktivna: stavka.id == aktivna.id,
                      naPritisak: () => naOdabir(stavka),
                    ),
                  const SizedBox(height: Razmaci.s),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Zaglavlje extends StatelessWidget {
  const _Zaglavlje();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(
        Razmaci.l,
        Razmaci.xl,
        Razmaci.l,
        Razmaci.l,
      ),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: Boje.navyTamniji)),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: Boje.primarna,
              borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            ),
            child: const Icon(
              Icons.two_wheeler,
              size: 20,
              color: Boje.naPrimarnoj,
            ),
          ),
          const SizedBox(width: Razmaci.m),
          const Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'SunnyRides',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                'Administracija',
                style: TextStyle(color: Boje.navyPrigusen, fontSize: 11),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _NaslovGrupe extends StatelessWidget {
  const _NaslovGrupe(this.tekst);

  final String tekst;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        Razmaci.l,
        Razmaci.l,
        Razmaci.l,
        Razmaci.s,
      ),
      child: Text(
        tekst,
        style: const TextStyle(
          color: Boje.navyPrigusen,
          fontSize: 10,
          fontWeight: FontWeight.w700,
          letterSpacing: 1.1,
        ),
      ),
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka({
    required this.stavka,
    required this.aktivna,
    required this.naPritisak,
  });

  final StavkaMenija stavka;
  final bool aktivna;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    // Aktivna stavka se prepoznaje po tri stvari odjednom - traci sa strane, tamnijoj
    // podlozi i boji teksta. Sama boja ne bi bila dovoljna na tamnoj podlozi.
    final boja = aktivna ? Boje.primarna : Boje.navyTekst;

    return Material(
      color: aktivna ? Boje.navyTamniji : Colors.transparent,
      child: InkWell(
        onTap: naPritisak,
        hoverColor: Colors.white.withValues(alpha: 0.04),
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: Razmaci.l,
            vertical: Razmaci.m,
          ),
          decoration: BoxDecoration(
            border: Border(
              left: BorderSide(
                color: aktivna ? Boje.primarna : Colors.transparent,
                width: 3,
              ),
            ),
          ),
          child: Row(
            children: [
              Icon(stavka.ikona, size: 19, color: boja),
              const SizedBox(width: Razmaci.m),
              Expanded(
                child: Text(
                  stavka.naziv,
                  style: TextStyle(
                    color: boja,
                    fontSize: 13.5,
                    fontWeight: aktivna ? FontWeight.w600 : FontWeight.w500,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
