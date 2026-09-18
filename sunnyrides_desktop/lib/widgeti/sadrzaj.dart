import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Tri stanja koja ima svaki ekran koji nesto dohvata: ucitavanje, greska i podaci.
///
/// Napisano jednom, da se na deset ekrana ne pojavi deset razlicitih vrtiljaka i
/// deset razlicitih nacina da se javi da server ne odgovara.
class Sadrzaj extends StatelessWidget {
  const Sadrzaj({
    super.key,
    required this.ucitavanje,
    required this.greska,
    required this.naPonovniPokusaj,
    required this.dijete,
  });

  final bool ucitavanje;
  final String? greska;
  final VoidCallback naPonovniPokusaj;
  final Widget dijete;

  @override
  Widget build(BuildContext context) {
    if (ucitavanje) {
      return const Center(child: CircularProgressIndicator());
    }

    if (greska != null) {
      return Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.cloud_off_outlined,
                size: 38,
                color: Boje.ivicaJaca,
              ),
              const SizedBox(height: Razmaci.l),
              Text(
                greska!,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Boje.tekstPrigusen,
                  fontSize: 13.5,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: Razmaci.l),
              OutlinedButton.icon(
                onPressed: naPonovniPokusaj,
                icon: const Icon(Icons.refresh, size: 18),
                label: const Text('Pokušaj ponovo'),
              ),
            ],
          ),
        ),
      );
    }

    return dijete;
  }
}

/// Poruka kad upit prodje, ali nema sta prikazati.
///
/// Razlikuje se od greske namjerno: prazna lista nije kvar, nego podatak.
class PrazanPopis extends StatelessWidget {
  const PrazanPopis({super.key, required this.poruka, this.ikona, this.akcija});

  final String poruka;
  final IconData? ikona;
  final Widget? akcija;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: Razmaci.xxl),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              ikona ?? Icons.inbox_outlined,
              size: 32,
              color: Boje.ivicaJaca,
            ),
            const SizedBox(height: Razmaci.m),
            Text(
              poruka,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 13.5),
            ),
            if (akcija != null) ...[const SizedBox(height: Razmaci.l), akcija!],
          ],
        ),
      ),
    );
  }
}
