import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Podnozje liste: koliko je prikazano od koliko, i kretanje po stranicama.
class Paginator extends StatelessWidget {
  const Paginator({
    super.key,
    required this.stranica,
    required this.velicinaStranice,
    required this.prikazano,
    required this.ukupno,
    required this.naStranicu,
  });

  /// Stranica se broji od nule, kao na serveru. Korisniku se prikazuje uvecana.
  final int stranica;

  final int velicinaStranice;
  final int prikazano;
  final int? ukupno;
  final ValueChanged<int> naStranicu;

  int get _ukupnoStranica {
    if (ukupno == null || ukupno == 0) {
      return 1;
    }

    return (ukupno! / velicinaStranice).ceil();
  }

  @override
  Widget build(BuildContext context) {
    final prvaNaStranici = stranica * velicinaStranice + 1;
    final zadnjaNaStranici = stranica * velicinaStranice + prikazano;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.karticaUnutra,
        vertical: Razmaci.m,
      ),
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: Boje.ivica)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              prikazano == 0
                  ? 'Nema zapisa'
                  : ukupno == null
                  ? 'Prikazano $prvaNaStranici–$zadnjaNaStranici'
                  : 'Prikazano $prvaNaStranici–$zadnjaNaStranici od ${Formati.broj(ukupno!)}',
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
            ),
          ),
          IconButton(
            tooltip: 'Prethodna stranica',
            onPressed: stranica > 0 ? () => naStranicu(stranica - 1) : null,
            icon: const Icon(Icons.chevron_left, size: 20),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: Razmaci.s),
            child: Text(
              '${stranica + 1} / $_ukupnoStranica',
              style: const TextStyle(
                fontSize: 12.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          IconButton(
            tooltip: 'Sljedeća stranica',
            onPressed: stranica + 1 < _ukupnoStranica
                ? () => naStranicu(stranica + 1)
                : null,
            icon: const Icon(Icons.chevron_right, size: 20),
          ),
        ],
      ),
    );
  }
}
