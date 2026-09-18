import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/pregled_poslovanja.dart';

/// Lista sa trakom po redu - koliko je vozila od ukupnog broja trenutno izdato.
class PresjekTrake extends StatelessWidget {
  const PresjekTrake({super.key, required this.stavke});

  final List<PresjekFlote> stavke;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < stavke.length; i++) ...[
          if (i > 0) const SizedBox(height: Razmaci.l),
          _Red(stavka: stavke[i]),
        ],
      ],
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({required this.stavka});

  final PresjekFlote stavka;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                stavka.naziv,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
            Text(
              '${stavka.uNajmu} / ${stavka.brojVozila}',
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
            ),
          ],
        ),
        const SizedBox(height: Razmaci.s),
        ClipRRect(
          borderRadius: BorderRadius.circular(Zaobljenja.pilula),
          child: LinearProgressIndicator(
            value: stavka.udio,
            minHeight: 7,
            backgroundColor: Boje.platno,
            valueColor: const AlwaysStoppedAnimation<Color>(Boje.primarna),
          ),
        ),
      ],
    );
  }
}
