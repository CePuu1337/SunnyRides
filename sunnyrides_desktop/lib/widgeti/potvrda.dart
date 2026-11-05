import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Dijalog potvrde za radnju koja se ne moze vratiti.
///
/// Upute traze potvrdu za svaku nepovratnu akciju. Ovdje je jedan oblik za sve: sta
/// ce se desiti, dugme koje odustaje i dugme koje radnju izvrsava. Vraca true samo
/// kad je korisnik izricito potvrdio; zatvaranje dijaloga je odustajanje.
Future<bool> potvrdiRadnju(
  BuildContext context, {
  required String naslov,
  required String tekst,
  required String potvrda,
  bool opasna = false,
}) async {
  final odgovor = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(naslov),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 440),
        child: Text(tekst, style: const TextStyle(height: 1.4)),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(true),
          style: opasna
              ? FilledButton.styleFrom(
                  backgroundColor: Boje.greska,
                  foregroundColor: Colors.white,
                )
              : null,
          child: Text(potvrda),
        ),
      ],
    ),
  );

  return odgovor ?? false;
}
