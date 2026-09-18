import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import 'obavjestenje.dart';

/// Okvir svakog dijaloga sa formom: naslov, sadrzaj, traka sa greskom i dugmad.
///
/// Napisano jednom da svaki unos u aplikaciji izgleda isto i da greska sa servera
/// uvijek zavrsi na istom mjestu - iznad dugmadi, gdje je korisnik i trazi.
class DijalogForme extends StatelessWidget {
  const DijalogForme({
    super.key,
    required this.naslov,
    required this.dijete,
    required this.naSnimanje,
    this.podnaslov,
    this.greska,
    this.uToku = false,
    this.natpisPotvrde = 'Sačuvaj',
    this.sirina = 640,
  });

  final String naslov;
  final String? podnaslov;
  final Widget dijete;
  final VoidCallback naSnimanje;
  final String? greska;
  final bool uToku;
  final String natpisPotvrde;
  final double sirina;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: sirina,
          maxHeight: MediaQuery.of(context).size.height * 0.88,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(
                Razmaci.xl,
                Razmaci.xl,
                Razmaci.l,
                Razmaci.l,
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          naslov,
                          style: const TextStyle(
                            fontSize: 17,
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
                  IconButton(
                    tooltip: 'Zatvori',
                    onPressed: uToku
                        ? null
                        : () => Navigator.of(context).pop(false),
                    icon: const Icon(Icons.close, size: 20),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(Razmaci.xl),
                child: dijete,
              ),
            ),
            if (greska != null)
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  Razmaci.xl,
                  0,
                  Razmaci.xl,
                  Razmaci.l,
                ),
                child: Obavjestenje.greska(greska!),
              ),
            const Divider(height: 1),
            Padding(
              padding: const EdgeInsets.all(Razmaci.l),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: uToku
                        ? null
                        : () => Navigator.of(context).pop(false),
                    child: const Text('Odustani'),
                  ),
                  const SizedBox(width: Razmaci.m),
                  ElevatedButton(
                    onPressed: uToku ? null : naSnimanje,
                    child: uToku
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Boje.naPrimarnoj,
                            ),
                          )
                        : Text(natpisPotvrde),
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

/// Dva polja u redu, sa razmakom. Forme se gotovo uvijek slazu u parovima.
class RedPolja extends StatelessWidget {
  const RedPolja({super.key, required this.lijevo, required this.desno});

  final Widget lijevo;
  final Widget desno;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(child: lijevo),
        const SizedBox(width: Razmaci.l),
        Expanded(child: desno),
      ],
    );
  }
}
