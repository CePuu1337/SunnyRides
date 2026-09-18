import 'package:flutter/material.dart';

import '../modeli/enumi.dart';
import 'boje.dart';
import 'razmaci.dart';

/// Obojena pilula sa nazivom statusa.
///
/// Boja nosi znacenje samo uz tekst, nikad umjesto njega - ko ne razlikuje zelenu
/// od crvene mora moci procitati sta pise.
class StatusnaPilula extends StatelessWidget {
  const StatusnaPilula({
    super.key,
    required this.tekst,
    required this.pozadina,
    required this.bojaTeksta,
    this.ikona,
  });

  final String tekst;
  final Color pozadina;
  final Color bojaTeksta;
  final IconData? ikona;

  factory StatusnaPilula.rezervacija(StatusRezervacije? status) {
    switch (status) {
      case StatusRezervacije.potvrdjena:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.uspjehPozadina,
          bojaTeksta: Boje.uspjehTekst,
        );
      case StatusRezervacije.naCekanju:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.upozorenjePozadina,
          bojaTeksta: Boje.upozorenjeTekst,
        );
      case StatusRezervacije.zavrsena:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.infoPozadina,
          bojaTeksta: Boje.infoTekst,
        );
      case StatusRezervacije.otkazana:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.greskaPozadina,
          bojaTeksta: Boje.greskaTekst,
        );
      case null:
        return const StatusnaPilula(
          tekst: 'Nepoznato',
          pozadina: Boje.neutralnoPozadina,
          bojaTeksta: Boje.neutralnoTekst,
        );
    }
  }

  factory StatusnaPilula.dozvola(StatusDozvole? status) {
    switch (status) {
      case StatusDozvole.odobrena:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.uspjehPozadina,
          bojaTeksta: Boje.uspjehTekst,
        );
      case StatusDozvole.naCekanju:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.upozorenjePozadina,
          bojaTeksta: Boje.upozorenjeTekst,
        );
      case StatusDozvole.odbijena:
        return StatusnaPilula(
          tekst: status!.naziv,
          pozadina: Boje.greskaPozadina,
          bojaTeksta: Boje.greskaTekst,
        );
      case null:
        return const StatusnaPilula(
          tekst: 'Nije predana',
          pozadina: Boje.neutralnoPozadina,
          bojaTeksta: Boje.neutralnoTekst,
        );
    }
  }

  /// Pilula koja govori je li rezervacija placena. Stoji uz statusnu, ne umjesto nje.
  factory StatusnaPilula.placeno(bool placeno) {
    return StatusnaPilula(
      tekst: placeno ? 'Plaćeno' : 'Neplaćeno',
      pozadina: placeno ? Boje.uspjehPozadina : Boje.upozorenjePozadina,
      bojaTeksta: placeno ? Boje.uspjehTekst : Boje.upozorenjeTekst,
      ikona: placeno ? Icons.check_circle_outline : Icons.schedule,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: Razmaci.m, vertical: Razmaci.xs),
      decoration: BoxDecoration(
        color: pozadina,
        borderRadius: BorderRadius.circular(Zaobljenja.pilula),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (ikona != null) ...[
            Icon(ikona, size: 14, color: bojaTeksta),
            const SizedBox(width: Razmaci.xs),
          ],
          Text(
            tekst,
            style: TextStyle(
              color: bojaTeksta,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
