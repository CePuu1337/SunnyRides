import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Bijela kartica sa naslovom, kakva se ponavlja kroz sve ekrane.
class Kartica extends StatelessWidget {
  const Kartica({
    super.key,
    required this.naslov,
    required this.dijete,
    this.podnaslov,
    this.akcija,
    this.bezUnutrasnjegRazmaka = false,
  });

  final String naslov;
  final String? podnaslov;

  /// Dugme ili veza u gornjem desnom uglu kartice.
  final Widget? akcija;

  final Widget dijete;

  /// Tabele idu do ivice kartice, pa im se unutrasnji razmak iskljucuje.
  final bool bezUnutrasnjegRazmaka;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              Razmaci.karticaUnutra,
              Razmaci.l,
              Razmaci.karticaUnutra,
              Razmaci.l,
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        naslov,
                        style: const TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      if (podnaslov != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          podnaslov!,
                          style: const TextStyle(
                            color: Boje.tekstPrigusen,
                            fontSize: 12.5,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                ?akcija,
              ],
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: bezUnutrasnjegRazmaka
                ? EdgeInsets.zero
                : const EdgeInsets.all(Razmaci.karticaUnutra),
            child: dijete,
          ),
        ],
      ),
    );
  }
}
