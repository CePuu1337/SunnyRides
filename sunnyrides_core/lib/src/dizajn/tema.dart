import 'package:flutter/material.dart';

import 'boje.dart';
import 'razmaci.dart';

/// Tema izvedena iz mockupa, zajednicka za obje aplikacije.
///
/// Razlika izmedju desktopa i telefona je gustina, ne izgled: isti su tonovi, ista
/// zaobljenja i isti razmaci, samo su elementi na desktopu zbijeniji jer se gleda
/// izbliza i mis je precizniji od prsta.
class Tema {
  const Tema._();

  static ThemeData desktop() => _osnovna(VisualDensity.compact);

  static ThemeData mobilna() => _osnovna(VisualDensity.standard);

  static ThemeData _osnovna(VisualDensity gustina) {
    final sema = ColorScheme.fromSeed(
      seedColor: Boje.primarna,
      brightness: Brightness.light,
    ).copyWith(
      primary: Boje.primarna,
      onPrimary: Boje.naPrimarnoj,
      secondary: Boje.navy,
      onSecondary: Colors.white,
      surface: Boje.povrsina,
      onSurface: Boje.tekst,
      error: Boje.greska,
      outline: Boje.ivica,
    );

    final osnova = ThemeData(useMaterial3: true, colorScheme: sema);

    return osnova.copyWith(
      visualDensity: gustina,
      scaffoldBackgroundColor: Boje.platno,
      dividerColor: Boje.ivica,

      textTheme: osnova.textTheme.apply(
        bodyColor: Boje.tekst,
        displayColor: Boje.tekst,
      ),

      appBarTheme: const AppBarTheme(
        backgroundColor: Boje.povrsina,
        foregroundColor: Boje.tekst,
        elevation: 0,
        scrolledUnderElevation: 0,
        surfaceTintColor: Colors.transparent,
      ),

      // Kartice nose ivicu umjesto sjene. U mockupu su ravne, a sjena bi na
      // sivoj podlozi napravila zamucen rub umjesto jasne linije.
      cardTheme: CardThemeData(
        color: Boje.povrsina,
        elevation: 0,
        margin: EdgeInsets.zero,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(Zaobljenja.kartica),
          side: const BorderSide(color: Boje.ivica),
        ),
      ),

      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: Boje.primarna,
          foregroundColor: Boje.naPrimarnoj,
          disabledBackgroundColor: Boje.ivica,
          disabledForegroundColor: Boje.tekstPrigusen,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: Razmaci.l, vertical: Razmaci.m),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
          ),
        ),
      ),

      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: Boje.tekstBlazi,
          side: const BorderSide(color: Boje.ivicaJaca),
          padding: const EdgeInsets.symmetric(horizontal: Razmaci.l, vertical: Razmaci.m),
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
          ),
        ),
      ),

      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: Boje.info,
          textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
        ),
      ),

      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Boje.povrsina,
        isDense: true,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: Razmaci.m,
          vertical: Razmaci.m,
        ),
        hintStyle: const TextStyle(color: Boje.tekstPrigusen),
        border: _ivicaPolja(Boje.ivicaJaca),
        enabledBorder: _ivicaPolja(Boje.ivicaJaca),
        focusedBorder: _ivicaPolja(Boje.primarna, debljina: 1.6),
        errorBorder: _ivicaPolja(Boje.greska),
        focusedErrorBorder: _ivicaPolja(Boje.greska, debljina: 1.6),
        disabledBorder: _ivicaPolja(Boje.ivica),
      ),

      dataTableTheme: DataTableThemeData(
        headingRowColor: WidgetStatePropertyAll(Boje.platno),
        headingTextStyle: const TextStyle(
          color: Boje.tekstPrigusen,
          fontWeight: FontWeight.w600,
          fontSize: 12,
          letterSpacing: 0.4,
        ),
        dataTextStyle: const TextStyle(color: Boje.tekst, fontSize: 13),
        dividerThickness: 1,
      ),

      chipTheme: ChipThemeData(
        backgroundColor: Boje.povrsina,
        selectedColor: Boje.primarnaSvijetla,
        side: const BorderSide(color: Boje.ivicaJaca),
        labelStyle: const TextStyle(fontSize: 13, color: Boje.tekstBlazi),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(Zaobljenja.pilula),
        ),
      ),

      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: Boje.navy,
        contentTextStyle: const TextStyle(color: Colors.white),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(Zaobljenja.dugme),
        ),
      ),

      dialogTheme: DialogThemeData(
        backgroundColor: Boje.povrsina,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        ),
      ),

      progressIndicatorTheme: const ProgressIndicatorThemeData(color: Boje.primarna),
    );
  }

  static OutlineInputBorder _ivicaPolja(Color boja, {double debljina = 1}) {
    return OutlineInputBorder(
      borderRadius: BorderRadius.circular(Zaobljenja.polje),
      borderSide: BorderSide(color: boja, width: debljina),
    );
  }
}
