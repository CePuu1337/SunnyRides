import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Pet zvjezdica, popunjenih do zadate ocjene.
class Zvjezdice extends StatelessWidget {
  const Zvjezdice({super.key, required this.ocjena, this.velicina = 16});

  final double ocjena;
  final double velicina;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (var i = 1; i <= 5; i++)
          Icon(
            i <= ocjena.round()
                ? Icons.star_rounded
                : Icons.star_outline_rounded,
            size: velicina,
            color: Boje.primarnaTamnija,
          ),
      ],
    );
  }
}

/// Zvjezdice na koje se moze kucnuti, za ostavljanje ocjene.
class OdabirOcjene extends StatelessWidget {
  const OdabirOcjene({
    super.key,
    required this.ocjena,
    required this.naPromjenu,
    this.velicina = 36,
  });

  final int ocjena;
  final ValueChanged<int> naPromjenu;
  final double velicina;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        for (var i = 1; i <= 5; i++)
          IconButton(
            onPressed: () => naPromjenu(i),
            iconSize: velicina,
            padding: const EdgeInsets.symmetric(horizontal: Razmaci.xs),
            constraints: const BoxConstraints(),
            icon: Icon(
              i <= ocjena ? Icons.star_rounded : Icons.star_outline_rounded,
              color: Boje.primarnaTamnija,
            ),
            tooltip: '$i',
          ),
      ],
    );
  }
}
