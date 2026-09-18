import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/pregled_poslovanja.dart';

/// Jedna od cetiri kartice na vrhu pocetnog ekrana.
class MetrikaKartica extends StatelessWidget {
  const MetrikaKartica({
    super.key,
    required this.naslov,
    required this.vrijednost,
    required this.ikona,
    required this.bojaIkone,
    this.pojasnjenje,
    this.poredba,
    this.poredbeniMjesec,
  });

  final String naslov;
  final String vrijednost;
  final IconData ikona;
  final Color bojaIkone;

  /// Sitni tekst ispod brojke - razlaganje ili jedinica.
  final String? pojasnjenje;

  /// Promjena u odnosu na prosli mjesec, ako za ovu metriku ima smisla.
  final PoredbaMetrike? poredba;
  final String? poredbeniMjesec;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(Razmaci.karticaUnutra),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                Container(
                  width: 34,
                  height: 34,
                  decoration: BoxDecoration(
                    color: bojaIkone.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(Zaobljenja.dugme),
                  ),
                  child: Icon(ikona, size: 19, color: bojaIkone),
                ),
                const SizedBox(width: Razmaci.m),
                Expanded(
                  child: Text(
                    naslov,
                    style: const TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: Razmaci.l),
            Text(
              vrijednost,
              style: const TextStyle(
                fontSize: 26,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.6,
              ),
            ),
            if (pojasnjenje != null) ...[
              const SizedBox(height: Razmaci.xs),
              Text(
                pojasnjenje!,
                style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
              ),
            ],
            if (poredba != null) ...[
              const SizedBox(height: Razmaci.m),
              _Promjena(poredba: poredba!, mjesec: poredbeniMjesec),
            ],
          ],
        ),
      ),
    );
  }
}

class _Promjena extends StatelessWidget {
  const _Promjena({required this.poredba, this.mjesec});

  final PoredbaMetrike poredba;
  final String? mjesec;

  @override
  Widget build(BuildContext context) {
    final promjena = poredba.promjenaPosto;

    // Kad proslog mjeseca nije bilo nicega, postotak ne postoji. Umjesto izmisljene
    // strelice pise se sta se zaista zna.
    if (promjena == null) {
      return const Text(
        'Prethodni period bez podataka',
        style: TextStyle(color: Boje.tekstPrigusen, fontSize: 11.5),
      );
    }

    final raste = promjena >= 0;
    final boja = raste ? Boje.uspjehTekst : Boje.greskaTekst;

    return Row(
      children: [
        Icon(
          raste ? Icons.arrow_upward : Icons.arrow_downward,
          size: 13,
          color: boja,
        ),
        const SizedBox(width: 2),
        Text(
          Formati.promjena(promjena),
          style: TextStyle(
            color: boja,
            fontSize: 11.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(width: Razmaci.xs),
        Expanded(
          child: Text(
            mjesec == null
                ? 'u odnosu na prošli mjesec'
                : 'u odnosu na $mjesec',
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 11.5),
          ),
        ),
      ],
    );
  }
}
