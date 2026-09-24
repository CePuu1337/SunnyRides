import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Obojena traka sa porukom. Boja i ikona idu zajedno, da poruka ima znacenje i
/// kad se boja ne vidi.
class Obavjestenje extends StatelessWidget {
  const Obavjestenje({
    super.key,
    required this.tekst,
    required this.pozadina,
    required this.bojaTeksta,
    required this.ikona,
    this.naslov,
  });

  final String tekst;
  final String? naslov;
  final Color pozadina;
  final Color bojaTeksta;
  final IconData ikona;

  factory Obavjestenje.greska(String tekst) => Obavjestenje(
    tekst: tekst,
    pozadina: Boje.greskaPozadina,
    bojaTeksta: Boje.greskaTekst,
    ikona: Icons.error_outline,
  );

  factory Obavjestenje.upozorenje(String tekst, {String? naslov}) =>
      Obavjestenje(
        tekst: tekst,
        naslov: naslov,
        pozadina: Boje.upozorenjePozadina,
        bojaTeksta: Boje.upozorenjeTekst,
        ikona: Icons.warning_amber_outlined,
      );

  factory Obavjestenje.info(String tekst, {String? naslov}) => Obavjestenje(
    tekst: tekst,
    naslov: naslov,
    pozadina: Boje.infoPozadina,
    bojaTeksta: Boje.infoTekst,
    ikona: Icons.info_outline,
  );

  factory Obavjestenje.uspjeh(String tekst) => Obavjestenje(
    tekst: tekst,
    pozadina: Boje.uspjehPozadina,
    bojaTeksta: Boje.uspjehTekst,
    ikona: Icons.check_circle_outline,
  );

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: pozadina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(ikona, size: 18, color: bojaTeksta),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (naslov != null) ...[
                  Text(
                    naslov!,
                    style: TextStyle(
                      color: bojaTeksta,
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                ],
                Text(
                  tekst,
                  style: TextStyle(
                    color: bojaTeksta,
                    fontSize: 12.5,
                    height: 1.45,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Tri stanja ekrana koji nesto dohvata, u mobilnom obliku.
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
        child: Padding(
          padding: const EdgeInsets.all(Razmaci.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.cloud_off_outlined,
                size: 36,
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
class PrazanPopis extends StatelessWidget {
  const PrazanPopis({super.key, required this.poruka, this.ikona, this.akcija});

  final String poruka;
  final IconData? ikona;
  final Widget? akcija;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(Razmaci.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              ikona ?? Icons.inbox_outlined,
              size: 34,
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
