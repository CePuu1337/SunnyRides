import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Obojena traka sa porukom - greska, upozorenje ili objasnjenje.
///
/// Boja i ikona idu zajedno, da poruka ima znacenje i kad se boja ne vidi.
class Obavjestenje extends StatelessWidget {
  const Obavjestenje({
    super.key,
    required this.tekst,
    required this.pozadina,
    required this.bojaTeksta,
    required this.ikona,
  });

  final String tekst;
  final Color pozadina;
  final Color bojaTeksta;
  final IconData ikona;

  factory Obavjestenje.greska(String tekst) => Obavjestenje(
    tekst: tekst,
    pozadina: Boje.greskaPozadina,
    bojaTeksta: Boje.greskaTekst,
    ikona: Icons.error_outline,
  );

  factory Obavjestenje.upozorenje(String tekst) => Obavjestenje(
    tekst: tekst,
    pozadina: Boje.upozorenjePozadina,
    bojaTeksta: Boje.upozorenjeTekst,
    ikona: Icons.warning_amber_outlined,
  );

  factory Obavjestenje.info(String tekst) => Obavjestenje(
    tekst: tekst,
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
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.l,
        vertical: Razmaci.m,
      ),
      decoration: BoxDecoration(
        color: pozadina,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(ikona, size: 18, color: bojaTeksta),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Text(
              tekst,
              style: TextStyle(color: bojaTeksta, fontSize: 13, height: 1.4),
            ),
          ),
        ],
      ),
    );
  }
}
