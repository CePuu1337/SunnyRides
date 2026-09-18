import 'package:flutter/material.dart';

/// Paleta iz mockupa, na jednom mjestu.
///
/// Boje se ne pisu direktno u ekranima. Kad se promijeni nijansa, mijenja se ovdje
/// i mijenja se svuda - inace ista narandzasta zavrsi u pet malo razlicitih verzija.
class Boje {
  const Boje._();

  /// Narandzasta kojom se oznacava glavna radnja i aktivna stavka u meniju.
  static const primarna = Color(0xFFF0B429);
  static const primarnaTamnija = Color(0xFFDE911D);
  static const primarnaSvijetla = Color(0xFFFDF3DC);

  /// Tekst na narandzastoj podlozi je taman, ne bijel - bijelo na zutom se ne cita.
  static const naPrimarnoj = Color(0xFF1F2937);

  /// Bocna traka.
  static const navy = Color(0xFF1F2937);
  static const navyTamniji = Color(0xFF111827);
  static const navyTekst = Color(0xFFD1D5DB);
  static const navyPrigusen = Color(0xFF9CA3AF);

  /// Podloga ekrana i kartica.
  static const platno = Color(0xFFF3F4F6);
  static const povrsina = Color(0xFFFFFFFF);
  static const ivica = Color(0xFFE5E7EB);
  static const ivicaJaca = Color(0xFFD1D5DB);

  static const tekst = Color(0xFF111827);
  static const tekstBlazi = Color(0xFF374151);
  static const tekstPrigusen = Color(0xFF6B7280);

  /// Statusi. Svaki par je pozadina pilule i boja teksta u njoj.
  static const uspjehPozadina = Color(0xFFDEF7EC);
  static const uspjehTekst = Color(0xFF03543F);

  static const upozorenjePozadina = Color(0xFFFEF3C7);
  static const upozorenjeTekst = Color(0xFF92400E);

  static const infoPozadina = Color(0xFFE1EFFE);
  static const infoTekst = Color(0xFF1E429F);

  static const greskaPozadina = Color(0xFFFDE8E8);
  static const greskaTekst = Color(0xFF9B1C1C);

  static const neutralnoPozadina = Color(0xFFF3F4F6);
  static const neutralnoTekst = Color(0xFF4B5563);

  /// Jace verzije istih boja, za ivice, ikone i trake u dijagramima.
  static const uspjeh = Color(0xFF057A55);
  static const upozorenje = Color(0xFFC27803);
  static const info = Color(0xFF1C64F2);
  static const greska = Color(0xFFE02424);
}
